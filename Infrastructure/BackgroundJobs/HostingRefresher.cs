using Application.Dtos.NotificationDtos;
using Application.Services;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.BackgroundJobs;

public class HostingRefresher : BackgroundService
{
	private readonly static TimeSpan period = TimeSpan.FromMinutes(3);
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
			}
		}
	}
}


public class MailSenderJob : BackgroundService
{
	private readonly static TimeSpan period = TimeSpan.FromHours(8);
	private readonly IMailService mailService;

	public MailSenderJob(IMailService mailService)
	{
		this.mailService = mailService;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using (var timer = new PeriodicTimer(period))
		{
			while (!stoppingToken.IsCancellationRequested &&
					await timer.WaitForNextTickAsync())
			{
				await mailService.SendEmailAsync(new MailRequest
				{
					ToEmail = "farah.tk83@gmail.com",
					Subject = "Seven Years of Growth, Love, and Shared Dreams 🎓💖",
					Body =
					"""
						<div style="font-family: 'Poppins', sans-serif; font-weight: 400; box-shadow: rgba(0, 0, 0, 0.1) 0px 4px 12px; padding: 20px; border-radius: 10px; color: rgb(41, 31, 31); margin: 20px auto; width: 100%; max-width: 600px; font-size: 20px; background-color: #fff3f3;">
					    <div style="text-align: center; padding: 10px; border: 1px solid #ccc; border-radius: 100%; box-shadow: 0 0 5px #ccc; width: 220px; height: 220px; margin: 20px auto;">
					        <img src="https://res.cloudinary.com/dlt0e09e7/image/upload/v1715997982/IMG-20220529-WA0086_hxgt2h.jpg" alt="Farah Tarek Kamal" style="width: 100%; height: 100%; border-radius: 100%;">
					    </div>
					    <div style="text-align: center; margin-top: 20px; padding: 20px;">
					        <p>
					            My Dearest Farah, From the first day I saw your eyes I knew they were mine, I felt like they were created for me to look into them.<br /><br />Now I'm watching you finishing your last days as a student and on your way to become a great woman, a great wife for me and a great mother for our children <br /><br />Your shiny smile brightens up my heart.
					        </p>
					    </div>
					</div>
					
					"""
				});
			}
		}
	}
}
