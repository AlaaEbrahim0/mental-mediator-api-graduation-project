using Application.Contracts;
using Application.Dtos.DepressionTestDtos;
using Domain.Errors;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Clients;

public class ChatContentModerator : IChatContentModerator
{
	private const string HttpClientName = "ml-client";
	private const string PredictPath = "/predict_HS";

	private readonly MLServiceProbe _probe;
	private readonly ILogger<ChatContentModerator> _logger;

	public ChatContentModerator(MLServiceProbe probe, ILogger<ChatContentModerator> logger)
	{
		_probe = probe;
		_logger = logger;
	}

	public async Task<Result> EnsureAllowedAsync(string content)
	{
		var probe = await _probe.PredictAsync<HateSpeechDetectionResult>(
			HttpClientName, PredictPath, new { text = content });

		if (probe.Status != MLProbeStatus.Completed)
		{
			// A slow, unreachable, or unreadable model is not a clean bill of
			// health: the message stays unsent rather than going out unmoderated.
			_logger.LogWarning("Chat moderation did not produce a decision ({Status}).", probe.Status);
			return ChatErrors.ModerationUnavailable();
		}

		if (string.IsNullOrWhiteSpace(probe.Value!.Prediction))
		{
			_logger.LogWarning("Chat moderation returned an empty response.");
			return ChatErrors.ModerationUnavailable();
		}

		return probe.Value.Prediction.Equals("normal", StringComparison.OrdinalIgnoreCase)
			? Result.Success()
			: ChatErrors.ContentRejected();
	}
}
