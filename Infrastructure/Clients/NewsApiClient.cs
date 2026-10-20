using System.Net.Http.Json;
using Application.Contracts;
using Application.Dtos.NewsDtos;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Clients;

public class NewsApiClient : INewsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NewsApiClient> _logger;

    public NewsApiClient(IHttpClientFactory httpClientFactory, ILogger<NewsApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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
        _logger.LogWarning("News API request failed: {StatusCode} - {Error}", (int)response.StatusCode, error);

        return Error.ServiceUnavailable("ExternalServices.NewsApiServiceUnavailable", "failed to fetch news");
    }
}
