using System.Net;
using System.Text;
using Application.Contracts;
using Domain.Errors;
using FluentAssertions;
using Infrastructure.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Covers the hosted content-safety service behaving badly the way it really
/// behaves: asleep and slow, answering with a gateway error, or answering with
/// something unreadable. Nothing here touches the network.
/// </summary>
public class ModerationClientTests
{
	private const string NormalJson = """{"prediction":"Normal"}""";
	private const string HateJson = """{"prediction":"Hate Speech"}""";

	// ---------------------------------------------------------------- helpers

	private sealed class StubHandler : HttpMessageHandler
	{
		private readonly Func<int, CancellationToken, Task<HttpResponseMessage>> _responder;

		public StubHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> responder)
			=> _responder = responder;

		public int Attempts { get; private set; }

		public HttpRequestMessage? LastRequest { get; private set; }

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			LastRequest = request;
			return await _responder(Attempts++, cancellationToken);
		}

		public static StubHandler Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
			new((_, _) => Task.FromResult(new HttpResponseMessage(status)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json"),
			}));

		/// <summary>Fails with the given statuses first, then succeeds.</summary>
		public static StubHandler FailsThenSucceeds(
			IReadOnlyCollection<HttpStatusCode> failures,
			string json) =>
			new((attempt, token) => attempt < failures.Count
				? Task.FromResult(new HttpResponseMessage(failures.ElementAt(attempt)))
				: RespondAfter(TimeSpan.Zero, json, token));

		public static StubHandler SlowThenJson(TimeSpan delay, string json) =>
			new((attempt, token) => attempt == 0
				? RespondAfter(delay, json, token)
				: Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(json, Encoding.UTF8, "application/json"),
				}));

		public static StubHandler AlwaysSlow(TimeSpan delay) =>
			new((_, token) => RespondAfter(delay, NormalJson, token));

		private static async Task<HttpResponseMessage> RespondAfter(
			TimeSpan delay,
			string json,
			CancellationToken token)
		{
			await Task.Delay(delay, token);
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json"),
			};
		}
	}

	private sealed class SingleClientFactory : IHttpClientFactory
	{
		private readonly HttpMessageHandler _handler;

		public SingleClientFactory(HttpMessageHandler handler) => _handler = handler;

		public HttpClient CreateClient(string name) =>
			new(_handler, disposeHandler: false) { BaseAddress = new Uri("https://ml.test") };
	}

	private static IConfiguration Config(
		int timeoutSeconds = 45,
		int maxRetries = 2,
		int retryBackoffSeconds = 0) =>
		new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["MLServer:TimeoutSeconds"] = timeoutSeconds.ToString(),
			["MLServer:MaxRetries"] = maxRetries.ToString(),
			["MLServer:RetryBackoffSeconds"] = retryBackoffSeconds.ToString(),
		}).Build();

	private static MLServiceProbe Probe(HttpMessageHandler handler, IConfiguration? configuration = null) =>
		new(new SingleClientFactory(handler), configuration ?? Config(), NullLogger<MLServiceProbe>.Instance);

	private static HateSpeechDetectorClient Detector(HttpMessageHandler handler, IConfiguration? config = null) =>
		new(Probe(handler, config), NullLogger<HateSpeechDetectorClient>.Instance);

	private static ChatContentModerator Moderator(HttpMessageHandler handler, IConfiguration? config = null) =>
		new(Probe(handler, config), NullLogger<ChatContentModerator>.Instance);

	// ------------------------------------------------------- the decision path

	[Fact]
	public async Task allows_content_the_model_marks_normal()
	{
		var handler = StubHandler.Json(NormalJson);

		var result = await Detector(handler).IsHateSpeech("Feeling better after a walk today.");

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeFalse();
	}

	[Fact]
	public async Task rejects_content_the_model_flags()
	{
		var handler = StubHandler.Json(HateJson);

		var result = await Detector(handler).IsHateSpeech("some flagged text");

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task calls_the_same_path_and_body_shape_as_before()
	{
		var handler = StubHandler.Json(NormalJson);

		await Detector(handler).IsHateSpeech("hello");

		handler.LastRequest!.RequestUri!.ToString().Should().Be("https://ml.test/predict_HS");
		handler.LastRequest.Method.Should().Be(HttpMethod.Post);
		(await handler.LastRequest.Content!.ReadAsStringAsync()).Should().Contain("hello");
		handler.Attempts.Should().Be(1);
	}

	// ---------------------------------------------------------- cold start

	[Fact]
	public async Task waits_through_a_cold_start_and_still_uses_the_answer()
	{
		// Render needs tens of seconds to wake. The budget has to be wide
		// enough that the caller gets a real decision instead of an error.
		var handler = StubHandler.SlowThenJson(TimeSpan.FromMilliseconds(400), NormalJson);

		var result = await Detector(handler).IsHateSpeech("calm text");

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeFalse();
		handler.Attempts.Should().Be(1);
	}

	[Fact]
	public async Task reports_unavailable_when_the_service_never_wakes()
	{
		var handler = StubHandler.AlwaysSlow(TimeSpan.FromSeconds(2));

		var result = await Detector(handler, Config(timeoutSeconds: 1))
			.IsHateSpeech("calm text");

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("ExternalServices.HateSpeechDetectionServiceUnavailable");
		result.Error.ErrorType.Should().Be(ErrorType.ServiceUnavailable);
	}

	[Fact]
	public async Task a_timeout_is_never_reported_as_safe_content()
	{
		// The dangerous failure mode: treating "no answer" as "no problem".
		var result = await Detector(StubHandler.AlwaysSlow(TimeSpan.FromSeconds(2)), Config(timeoutSeconds: 1))
			.IsHateSpeech("anything");

		result.IsSuccess.Should().BeFalse("an unverified submission must not clear moderation");
	}

	// ------------------------------------------------------------- retries

	[Fact]
	public async Task retries_a_starting_instance_and_then_succeeds()
	{
		var handler = StubHandler.FailsThenSucceeds(
			[HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable],
			NormalJson);

		var result = await Detector(handler, Config(retryBackoffSeconds: 0)).IsHateSpeech("calm text");

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeFalse();
		handler.Attempts.Should().Be(3);
	}

	[Fact]
	public async Task gives_up_after_the_bounded_retry_count()
	{
		var handler = StubHandler.FailsThenSucceeds(
			[HttpStatusCode.BadGateway, HttpStatusCode.BadGateway, HttpStatusCode.BadGateway],
			NormalJson);

		var result = await Detector(handler, Config(maxRetries: 2, retryBackoffSeconds: 0))
			.IsHateSpeech("calm text");

		result.IsFailure.Should().BeTrue();
		handler.Attempts.Should().Be(3, "two retries on top of the first attempt");
	}

	[Fact]
	public async Task does_not_retry_a_deterministic_client_error()
	{
		var handler = StubHandler.Json("{}", HttpStatusCode.BadRequest);

		var result = await Detector(handler, Config(maxRetries: 2)).IsHateSpeech("calm text");

		result.IsFailure.Should().BeTrue();
		handler.Attempts.Should().Be(1);
	}

	// --------------------------------------------------- broken responses

	[Fact]
	public async Task fails_closed_on_a_body_that_is_not_the_expected_shape()
	{
		var handler = StubHandler.Json("""{"result":"unrecognised"}""");

		var result = await Detector(handler).IsHateSpeech("calm text");

		result.IsFailure.Should().BeTrue();
		result.Error.ErrorType.Should().Be(ErrorType.ServiceUnavailable);
	}

	[Fact]
	public async Task fails_closed_on_an_empty_prediction()
	{
		var handler = StubHandler.Json("""{"prediction":""}""");

		var result = await Detector(handler).IsHateSpeech("calm text");

		result.IsFailure.Should().BeTrue();
	}

	[Fact]
	public async Task fails_closed_on_a_blank_body()
	{
		var handler = StubHandler.Json("");

		var result = await Detector(handler).IsHateSpeech("calm text");

		result.IsFailure.Should().BeTrue();
	}

	[Fact]
	public async Task reports_unreachable_service_instead_of_throwing()
	{
		// The previous implementation let HttpRequestException escape, which
		// reached the browser as an HTTP 500 with a stack trace.
		var handler = new ConnectionRefusedHandler();

		var result = await Detector(handler, Config(maxRetries: 0)).IsHateSpeech("calm text");

		result.IsFailure.Should().BeTrue();
		result.Error.ErrorType.Should().Be(ErrorType.ServiceUnavailable);
	}

	// ----------------------------------------------------------------- chat

	[Fact]
	public async Task chat_allows_normal_content()
	{
		var result = await Moderator(StubHandler.Json(NormalJson)).EnsureAllowedAsync("how is everyone?");

		result.IsSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task chat_rejects_flagged_content_with_the_existing_error()
	{
		var result = await Moderator(StubHandler.Json(HateJson)).EnsureAllowedAsync("flagged");

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Chat.ContentRejected");
	}

	[Fact]
	public async Task chat_blocks_sending_when_the_service_is_cold()
	{
		var result = await Moderator(StubHandler.AlwaysSlow(TimeSpan.FromSeconds(2)), Config(timeoutSeconds: 1))
			.EnsureAllowedAsync("hello");

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be(ChatErrors.ModerationUnavailable().Code);
	}

	[Fact]
	public async Task chat_moderation_never_throws_when_the_service_is_unreachable()
	{
		var result = await Moderator(new ConnectionRefusedHandler(), Config(maxRetries: 0))
			.EnsureAllowedAsync("hello");

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be(ChatErrors.ModerationUnavailable().Code);
	}

	// ----------------------------------------------------------- depression

	[Fact]
	public async Task depression_check_returns_the_mapped_label()
	{
		var handler = StubHandler.Json("""{"prediction":"negative"}""");
		var client = new DepressionDetectorClient(Probe(handler), NullLogger<DepressionDetectorClient>.Instance);

		var result = await client.IsDepressed(new Application.Dtos.DepressionTestDtos.DepressionTestRequest
		{
			Text = "i cannot sleep anymore",
		});

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().Be("Negative");
	}

	[Fact]
	public async Task depression_check_fails_gently_while_the_service_is_asleep()
	{
		var handler = StubHandler.AlwaysSlow(TimeSpan.FromSeconds(2));
		var client = new DepressionDetectorClient(
			Probe(handler, Config(timeoutSeconds: 1)),
			NullLogger<DepressionDetectorClient>.Instance);

		var result = await client.IsDepressed(
			new Application.Dtos.DepressionTestDtos.DepressionTestRequest { Text = "tired" });

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("ExternalServices.DepressionTestServiceUnavailable");
	}

	/// <summary>Stands in for a service that is down or unreachable.</summary>
	private sealed class ConnectionRefusedHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken) =>
			Task.FromException<HttpResponseMessage>(
				new HttpRequestException("Connection refused (simulated)"));
	}
}
