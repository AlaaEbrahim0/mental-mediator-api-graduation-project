using Application.Contracts;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Shared;

namespace Application.Services;

public class NotificationService : INotificationService
{
	private readonly IRepositoryManager _repos;
	private readonly IUserClaimsService _userClaimsService;
	private readonly INotificationSender _notificationSender;
	private readonly IMapper _mapper;

	public NotificationService(IRepositoryManager repos, IUserClaimsService userClaimsService, IMapper mapper, INotificationSender notificationSender)
	{
		_repos = repos;
		_userClaimsService = userClaimsService;
		_mapper = mapper;
		_notificationSender = notificationSender;
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

	public async Task SendNotificationAsync(Notification notification)
	{
		await _notificationSender.SendNotificationAsync(notification);
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



}
