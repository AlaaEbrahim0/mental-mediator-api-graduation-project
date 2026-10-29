using Application.Contracts;
using Application.Dtos.NotificationDtos;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Services;

public class NotificationSender : INotificationSender
{
	private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
	private readonly IMapper _mapper;
	private readonly IRepositoryManager _repos;
	private readonly ILogger<NotificationSender> _logger;

	public NotificationSender(
		IHubContext<NotificationHub, INotificationClient> hubContext,
		IMapper mapper,
		IRepositoryManager repos,
		ILogger<NotificationSender> logger)
	{
		_hubContext = hubContext;
		_mapper = mapper;
		_repos = repos;
		_logger = logger;
	}

	public async Task SendNotificationAsync(Notification notification)
	{
		// Persist the notification BEFORE attempting real-time delivery so that
		// it remains the source of truth even if SignalR fails or the client
		// is disconnected. Disconnected clients can always recover it from the
		// database via the GET endpoints.
		_repos.Notifications.CreateNotification(notification);
		await _repos.SaveAsync();

		var notificationResponse = _mapper.Map<NotificationResponse>(notification);

		try
		{
			await _hubContext
				.Clients
				.User(notification.AppUserId)
				.ReceiveNotification(notificationResponse);
		}
		catch (Exception ex)
		{
			// Do not let SignalR failures discard the persisted notification.
			// The notification is already saved; the client can retrieve it on
			// next connection or poll via GET /api/notifications.
			_logger.LogWarning(
				ex,
				"Real-time delivery of notification {NotificationId} to user {UserId} failed; record is persisted",
				notification.Id,
				notification.AppUserId);
		}
	}
}
