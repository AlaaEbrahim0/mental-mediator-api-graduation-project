using System.Net.Http.Json;
using Application.Contracts;
using Application.Dtos;
using Application.Dtos.NewsDtos;
using Application.Dtos.WeeklyScheduleDtos;
using Shared;

namespace Infrastructure.Clients;

public class DepressionDetectorClient : IDepressionDetector
{
	private readonly IHttpClientFactory _httpClientFactory;

	public DepressionDetectorClient(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	public async Task<Result<bool>> IsDepressed(DepressionTestRequest request)
	{
		using var httpClient = _httpClientFactory.CreateClient("ml-client");
		var response = await httpClient.PostAsJsonAsync("/predict_DP", request);
		if (response.IsSuccessStatusCode)
		{
			var data = await response.Content.ReadFromJsonAsync<DepressionTestResult>();
			if (data!.Prediction.Equals("normal", StringComparison.InvariantCultureIgnoreCase))
			{
				return false;
			}
			return true;
		}
		return Error.ServiceUnavailable("ExternalServices.DepressionTestServiceUnavailable", "failed to fetch data from ml server");
	}
}

public class NewsApiClient : INewsService
{
	private readonly IHttpClientFactory _httpClientFactory;

	public NewsApiClient(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	public async Task<Result<NewsResponse?>> GetNews(NewsRequestParameters parameters)
	{
		using var httpClient = _httpClientFactory.CreateClient("news-api-client");

		var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
		query["q"] = parameters.Query;
		query["pageSize"] = parameters.PageSize.ToString();
		query["page"] = parameters.Page.ToString();
		query["language"] = parameters.Language;

		var requestUri = $"everything?{query}";

		var response = await httpClient.GetAsync(requestUri);
		if (response.IsSuccessStatusCode)
		{
			var data = await response.Content.ReadFromJsonAsync<NewsResponse>();
			return data;
		}

		var error = await response.Content.ReadAsStringAsync();
		Console.WriteLine(error);

		return Error.ServiceUnavailable("ExternalServices.NewsApiServiceUnavailable", "failed to fetch news");
	}

}