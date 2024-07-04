using System.Net.Http.Json;
using Application.Contracts;
using Application.Dtos.NewsDtos;
using Shared;

namespace Infrastructure.Clients;

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