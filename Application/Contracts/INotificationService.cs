using Shared;

namespace Application.Contracts;
public interface INotificationService : INotificationSender
{
	Task<Result<IEnumerable<NotificationResponse>>> GetNotificationByUserId(string userId);
	Task<Result<NotificationResponse>> GetNotificationById(int id);
}
