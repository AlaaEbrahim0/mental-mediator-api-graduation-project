using Domain.Entities;
using Shared;

namespace Application.Contracts;
public interface INotificationService
{
	Task<Result<IEnumerable<NotificationResponse>>> GetNotificationByUserId(string userId);
	Task<Result<NotificationResponse>> GetNotificationById(int id);
	Task SendNotificationAsync(Notification notification);
	Task<Result<IEnumerable<NotificationResponse>>> GetCurrentUserNotifications();


}
