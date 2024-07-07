using Application.Dtos.NotificationDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
	public override async Task OnConnectedAsync()
	{
		var name = Context.User?.FindFirst("name")?.Value;
		await Clients
			.Caller
			.ReceiveMessage("Hi: " + name);

		await Clients
			.Caller
			.ReceiveMessage("You ConnectionId: " + Context.ConnectionId);

		await base.OnConnectedAsync();
	}
}

public interface INotificationClient
{
	Task ReceiveNotification(NotificationResponse notification);
	Task ReceiveMessage(string message);
}