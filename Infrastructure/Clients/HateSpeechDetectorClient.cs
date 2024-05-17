using System.Net.Http.Json;
using Application.Contracts;
using Application.Dtos.WeeklyScheduleDtos;
using Shared;

namespace Infrastructure.Clients;


public class HateSpeechDetectorClient : IHateSpeechDetector
{
	private readonly IHttpClientFactory _httpClientFactory;

	public HateSpeechDetectorClient(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	public async Task<Result<bool>> IsHateSpeech(string content)
	{
		using (var httpClient = _httpClientFactory.CreateClient("ml-client"))
		{
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
			return Error.ServiceUnavailable("ExternalServices.HateSpeechDetectionServiceUnavailable", "failed to fetch data from ml server");

		}

	}


}
