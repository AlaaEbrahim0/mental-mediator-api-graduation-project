using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.BackgroundJobs;

public class HostingRefresher : BackgroundService
{
	private readonly static TimeSpan period = TimeSpan.FromMinutes(5);
	private readonly IHttpClientFactory _httpClientFactory;

	public HostingRefresher(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using (var timer = new PeriodicTimer(period))
		{
			while (!stoppingToken.IsCancellationRequested &&
					await timer.WaitForNextTickAsync())
			{
				using (var httpClient = _httpClientFactory.CreateClient("self"))
				{
					Console.WriteLine(httpClient.BaseAddress);
					try
					{
						var response = await httpClient.GetAsync("api/posts");

						if (response.IsSuccessStatusCode)
						{
							Console.WriteLine("Refreshed...");
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
				}
				using (var httpClient = _httpClientFactory.CreateClient("ml-client"))
				{
					Console.WriteLine(httpClient.BaseAddress);
					try
					{
						var response = await httpClient.PostAsJsonAsync("predict_HS", new { text = "normal" });

						if (response.IsSuccessStatusCode)
						{
							Console.WriteLine("Refreshed...");
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
				}
			}
		}
	}
}


