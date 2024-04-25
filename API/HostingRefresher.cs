namespace API;

public class HostingRefresher : BackgroundService
{
	private readonly static TimeSpan period = TimeSpan.FromSeconds(5);
	private readonly IHttpClientFactory _httpClientFactory;

	public HostingRefresher(IHttpClientFactory httpClientFactory)
	{
		_httpClientFactory = httpClientFactory;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using (var timer = new PeriodicTimer(period))
		{
			while (!stoppingToken.IsCancellationRequested ||
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
			}
		}
	}
}
