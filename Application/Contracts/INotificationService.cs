using Application.Dtos.NotificationDtos;
using Domain.Entities;
using Shared;
using Shared.RequestParameters;

namespace Application.Contracts;
public interface INotificationService
{
	Task<Result<IEnumerable<NotificationResponse>>> GetNotificationByUserId(string userId, RequestParameters request);
	Task<Result<NotificationResponse>> GetNotificationById(int id);
	Task SendNotificationAsync(Notification notification);
	Task<Result<IEnumerable<NotificationResponse>>> GetCurrentUserNotifications(RequestParameters request);
	Task<Result<bool>> MarkAllAsReadAsync();
	Task<Result<bool>> MarkAsReadAsync(int id);
}
