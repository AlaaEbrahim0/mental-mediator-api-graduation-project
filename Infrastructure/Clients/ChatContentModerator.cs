using System.Net.Http.Json;
using System.Text.Json;
using Application.Contracts;
using Application.Dtos.DepressionTestDtos;
using Domain.Errors;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Clients;

public class ChatContentModerator : IChatContentModerator
{
	private const string HttpClientName = "chat-ml-client";

	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<ChatContentModerator> _logger;

	public ChatContentModerator(IHttpClientFactory httpClientFactory, ILogger<ChatContentModerator> logger)
	{
		_httpClientFactory = httpClientFactory;
		_logger = logger;
	}

	public async Task<Result> EnsureAllowedAsync(string content)
	{
		try
		{
			using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
			using var response = await httpClient.PostAsJsonAsync("/predict_HS", new { text = content });

			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning(
					"Chat moderation request failed with status code {StatusCode}.",
					(int)response.StatusCode);

				return ChatErrors.ModerationUnavailable();
			}

			var result = await response.Content.ReadFromJsonAsync<HateSpeechDetectionResult>();
			if (result is null || string.IsNullOrWhiteSpace(result.Prediction))
			{
				_logger.LogWarning("Chat moderation returned an empty response.");
				return ChatErrors.ModerationUnavailable();
			}

			if (result.Prediction.Equals("normal", StringComparison.OrdinalIgnoreCase))
			{
				return Result.Success();
			}

			return ChatErrors.ContentRejected();
		}
		catch (OperationCanceledException)
		{
			// The named client has a short timeout. A slow or unavailable model
			// must fail closed rather than let unmoderated content through.
			_logger.LogWarning("Chat moderation timed out.");
			return ChatErrors.ModerationUnavailable();
		}
		catch (HttpRequestException)
		{
			_logger.LogWarning("Chat moderation is unreachable.");
			return ChatErrors.ModerationUnavailable();
		}
		catch (JsonException)
		{
			_logger.LogWarning("Chat moderation returned an unreadable response.");
			return ChatErrors.ModerationUnavailable();
		}
		catch (InvalidOperationException)
		{
			_logger.LogWarning("Chat moderation is not configured correctly.");
			return ChatErrors.ModerationUnavailable();
		}
		catch (UriFormatException)
		{
			_logger.LogWarning("Chat moderation is not configured correctly.");
			return ChatErrors.ModerationUnavailable();
		}
		catch (NotSupportedException)
		{
			_logger.LogWarning("Chat moderation returned an unsupported response.");
			return ChatErrors.ModerationUnavailable();
		}
		catch (IOException)
		{
			_logger.LogWarning("Chat moderation response could not be read.");
			return ChatErrors.ModerationUnavailable();
		}
	}
}
