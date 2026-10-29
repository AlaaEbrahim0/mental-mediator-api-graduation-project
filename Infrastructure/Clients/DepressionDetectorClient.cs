using Application.Contracts;
using Application.Dtos.DepressionTestDtos;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Clients;

public class DepressionDetectorClient : IDepressionDetector
{
    private const string HttpClientName = "ml-client";
    private const string PredictPath = "/predict_DP";

    private readonly MLServiceProbe _probe;
    private readonly ILogger<DepressionDetectorClient> _logger;

    public DepressionDetectorClient(MLServiceProbe probe, ILogger<DepressionDetectorClient> logger)
    {
        _probe = probe;
        _logger = logger;
    }

    public async Task<Result<string>> IsDepressed(DepressionTestRequest request)
    {
        var probe = await _probe.PredictAsync<DepressionTestResponse>(
            HttpClientName, PredictPath, request);

        if (probe.Status != MLProbeStatus.Completed || string.IsNullOrWhiteSpace(probe.Value?.Prediction))
        {
            if (probe.Status == MLProbeStatus.Completed)
            {
                _logger.LogWarning("Depression detection returned no prediction.");
            }

            return Error.ServiceUnavailable(
                "ExternalServices.DepressionTestServiceUnavailable",
                "The check could not be completed right now. Please try again in a moment.");
        }

        if (probe.Value.Prediction.Equals("normal", StringComparison.InvariantCultureIgnoreCase))
        {
            return "Normal";
        }

        return probe.Value.Prediction.Equals("negative", StringComparison.InvariantCultureIgnoreCase)
            ? "Negative"
            : "Depressed";
    }
}
