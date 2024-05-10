using Application.Contracts;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Infrastructure.Services;
public class NotificationSender : INotificationSender
{
	private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
	private readonly IMapper _mapper;

	public NotificationSender(IHubContext<NotificationHub, INotificationClient> hubContext, IMapper mapper)
	{
		_hubContext = hubContext;
		_mapper = mapper;
	}

	public async Task SendNotificationAsync(Notification notification)
	{
		var notificationResponse = _mapper.Map<NotificationResponse>(notification);
		//await _hubContext.Clients.All.ReceiveNotification(notificationResponse);

		await _hubContext.Clients.User(notification.AppUserId).ReceiveNotification(notificationResponse);

	}

}

