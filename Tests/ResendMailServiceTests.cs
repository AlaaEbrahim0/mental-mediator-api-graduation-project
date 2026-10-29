using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Contracts;
using Application.Dtos.NotificationDtos;
using Application.Options;
using Domain.Errors;
using FluentAssertions;
using Infrastructure.Clients;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Tests for the Resend mail service: success when the provider
/// accepts a message, failure when it does not, and graceful
/// fallback to SMTP when Resend is unconfigured.
/// </summary>
public class ResendMailServiceTests
{
	private const string TestApiKey = "re_test_key";
	private const string TestFromEmail = "onboarding@resend.dev";
	private const string TestFromName = "Test App";

	private sealed class StubResendClient : IResendClient
	{
		public ResendSendResponse? Response { get; init; }
		public bool Throws { get; init; }
		public string? CapturedTo { get; private set; }
		public string? CapturedSubject { get; private set; }
		public string? CapturedHtml { get; private set; }
		public string? CapturedFrom { get; private set; }

		public Task<ResendSendResponse?> SendAsync(
			string from, string to, string subject, string html,
			System.Threading.CancellationToken ct = default)
		{
			CapturedFrom = from;
			CapturedTo = to;
			CapturedSubject = subject;
			CapturedHtml = html;

			if (Throws)
				throw new System.Exception("simulated provider crash");

			return Task.FromResult(Response);
		}
	}

	private static ResendSettings Settings(bool configured = true) => new()
	{
		ApiKey = configured ? TestApiKey : null,
		FromEmail = TestFromEmail,
		FromName = configured ? TestFromName : null,
	};

	private static MailRequest SampleRequest => new()
	{
		ToEmail = "recipient@example.com",
		Subject = "Test Subject",
		Body = "<p>Test body</p>"
	};

	private static IMailService CreateService(
		StubResendClient resendClient,
		bool resendConfigured = true,
		IMailService? fallback = null) =>
		new ResendMailService(
			resendClient,
			Options.Create(Settings(resendConfigured)),
			fallback ?? new FakeFallbackMailService(),
			NullLogger<ResendMailService>.Instance);

	[Fact]
	public async Task returns_success_when_resend_accepts_the_message()
	{
		var client = new StubResendClient
		{
			Response = new ResendSendResponse { Id = "msg_123" }
		};
		var service = CreateService(client);

		var result = await service.SendEmailAsync(SampleRequest);

		result.IsSuccess.Should().BeTrue();
		client.CapturedTo.Should().Be("recipient@example.com");
		client.CapturedSubject.Should().Be("Test Subject");
		client.CapturedHtml.Should().Be("<p>Test body</p>");
		client.CapturedFrom.Should().Be($"{TestFromName} <{TestFromEmail}>");
	}

	[Fact]
	public async Task returns_failure_when_resend_rejects_the_message()
	{
		var client = new StubResendClient
		{
			Response = null // provider returned an error → no id
		};
		var service = CreateService(client);

		var result = await service.SendEmailAsync(SampleRequest);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Mail.DeliveryFailed");
	}

	[Fact]
	public async Task returns_failure_when_resend_returns_empty_id()
	{
		var client = new StubResendClient
		{
			Response = new ResendSendResponse { Id = "" }
		};
		var service = CreateService(client);

		var result = await service.SendEmailAsync(SampleRequest);

		result.IsFailure.Should().BeTrue();
	}

	[Fact]
	public async Task returns_failure_when_provider_throws()
	{
		var client = new StubResendClient { Throws = true };
		var service = CreateService(client);

		var result = await service.SendEmailAsync(SampleRequest);

		result.IsFailure.Should().BeTrue();
	}

	[Fact]
	public async Task falls_back_to_smtp_when_resend_is_unconfigured()
	{
		var fallbackCalled = false;
		var fallback = new FakeFallbackMailService(() => fallbackCalled = true);

		var client = new StubResendClient
		{
			Response = new ResendSendResponse { Id = "msg_123" }
		};
		var service = CreateService(client, resendConfigured: false, fallback: fallback);

		var result = await service.SendEmailAsync(SampleRequest);

		result.IsSuccess.Should().BeTrue();
		fallbackCalled.Should().BeTrue();
		// Resend client should not have been called.
		client.CapturedTo.Should().BeNull();
	}

	[Fact]
	public async Task does_not_fall_back_to_smtp_when_resend_is_configured_but_rejects()
	{
		var fallbackCalled = false;
		var fallback = new FakeFallbackMailService(() => fallbackCalled = true);

		var client = new StubResendClient { Response = null };
		var service = CreateService(client, resendConfigured: true, fallback: fallback);

		var result = await service.SendEmailAsync(SampleRequest);

		result.IsFailure.Should().BeTrue();
		fallbackCalled.Should().BeFalse();
	}

	/// <summary>Simple fake SMTP fallback used to verify the fallback path.</summary>
	private sealed class FakeFallbackMailService : IMailService
	{
		private readonly Action? _onSend;

	 public FakeFallbackMailService(Action? onSend = null) => _onSend = onSend;

		public Task<Result> SendEmailAsync(MailRequest mailRequest)
		{
			_onSend?.Invoke();
			return Task.FromResult(Result.Success());
		}
	}
}
