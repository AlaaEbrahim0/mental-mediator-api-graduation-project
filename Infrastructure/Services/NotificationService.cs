using Application.Contracts;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Infrastructure.Services;
public class NotificationService : INotificationService
{
	private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
	private readonly IRepositoryManager _repos;
	private readonly IUserClaimsService _userClaimsService;
	private readonly IMapper _mapper;

	public NotificationService(IHubContext<NotificationHub, INotificationClient> hubContext, IRepositoryManager repos, IUserClaimsService userClaimsService, IMapper mapper)
	{
		_hubContext = hubContext;
		_repos = repos;
		_userClaimsService = userClaimsService;
		_mapper = mapper;
	}


	public async Task<Result<NotificationResponse>> GetNotificationById(int id)
	{
		var notification = await _repos.Notifications.GetById(id, false);
		if (notification is null)
		{
			return NotificationErrors.NotFound(id);
		}
		var notificationReponse = _mapper.Map<NotificationResponse>(notification);
		return notificationReponse;
	}

	public async Task<Result<IEnumerable<NotificationResponse>>> GetNotificationByUserId(string userId)
	{
		var currentUserId = _userClaimsService.GetUserId();
		if (userId != currentUserId)
		{
			return NotificationErrors.Forbidden();
		}
		var notifications = await _repos.Notifications.GetByUserId(userId, false);
		var notificationReponse = _mapper.Map<IEnumerable<NotificationResponse>>(notifications);
		return notificationReponse.ToList();
	}

	public async Task SendNotificationAsync(Notification notification)
	{
		var notificationResponse = _mapper.Map<NotificationResponse>(notification);
		await _hubContext.Clients.All.ReceiveNotification(notificationResponse);

	}


}
