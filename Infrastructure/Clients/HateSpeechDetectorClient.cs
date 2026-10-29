using Application.Contracts;
using Application.Dtos.DepressionTestDtos;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Clients;

public class HateSpeechDetectorClient : IHateSpeechDetector
{
    private const string HttpClientName = "ml-client";
    private const string PredictPath = "/predict_HS";

    private readonly MLServiceProbe _probe;
    private readonly ILogger<HateSpeechDetectorClient> _logger;

    public HateSpeechDetectorClient(MLServiceProbe probe, ILogger<HateSpeechDetectorClient> logger)
    {
        _probe = probe;
        _logger = logger;
    }

    public async Task<Result<bool>> IsHateSpeech(string content)
    {
        var probe = await _probe.PredictAsync<HateSpeechDetectionResult>(
            HttpClientName, PredictPath, new { text = content });

        if (probe.Status == MLProbeStatus.Completed)
        {
            // Only an explicit "normal" clears content. An absent or blank
            // prediction is not evidence of safety.
            if (string.IsNullOrWhiteSpace(probe.Value!.Prediction))
            {
                _logger.LogWarning("Hate speech detection returned no prediction.");
                return Unavailable();
            }

            return probe.Value.Prediction.Equals("normal", StringComparison.InvariantCultureIgnoreCase)
                ? false
                : true;
        }

        return Unavailable();
    }

    private static Error Unavailable() => Error.ServiceUnavailable(
        "ExternalServices.HateSpeechDetectionServiceUnavailable",
        "The content safety check could not be completed, so your content was not published. Please try again.");
}
