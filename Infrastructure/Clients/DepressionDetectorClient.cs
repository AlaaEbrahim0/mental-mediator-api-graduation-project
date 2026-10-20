using System.Net.Http.Json;
using Application.Contracts;
using Application.Dtos.DepressionTestDtos;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Clients;

public class DepressionDetectorClient : IDepressionDetector
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DepressionDetectorClient> _logger;

    public DepressionDetectorClient(IHttpClientFactory httpClientFactory, ILogger<DepressionDetectorClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<string>> IsDepressed(DepressionTestRequest request)
    {
        using var httpClient = _httpClientFactory.CreateClient("ml-client");
        var response = await httpClient.PostAsJsonAsync("/predict_DP", request);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<DepressionTestResponse>();
            if (data!.Prediction!.Equals("normal", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Normal";
            }
            if (data.Prediction.Equals("negative", StringComparison.InvariantCultureIgnoreCase))
            {
                return "Negative";
            }
            return "Depressed";
        }

        _logger.LogWarning("Depression detection request failed with status code: {StatusCode}", (int)response.StatusCode);
        return Error.ServiceUnavailable("ExternalServices.DepressionTestServiceUnavailable", "failed to fetch data from ml server");
    }
}
