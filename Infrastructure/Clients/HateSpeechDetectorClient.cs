using System.Net.Http.Json;
using Application.Contracts;
using Application.Dtos.DepressionTestDtos;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Clients;

public class HateSpeechDetectorClient : IHateSpeechDetector
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HateSpeechDetectorClient> _logger;

    public HateSpeechDetectorClient(IHttpClientFactory httpClientFactory, ILogger<HateSpeechDetectorClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<bool>> IsHateSpeech(string content)
    {
        using var httpClient = _httpClientFactory.CreateClient("ml-client");
        var response = await httpClient.PostAsJsonAsync("/predict_HS", new { text = content });
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<HateSpeechDetectionResult>();
            if (data!.Prediction.Equals("normal", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }
            return true;
        }

        _logger.LogWarning("HateSpeech detection request failed with status code: {StatusCode}", (int)response.StatusCode);
        return Error.ServiceUnavailable("ExternalServices.HateSpeechDetectionServiceUnavailable", "failed to fetch data from ml server");
    }
}
