using Domain.Entities;
using Shared;

namespace Domain.Repositories;

public interface INotificationRepository
{
	Task<Notification?> GetById(int notificationId, bool trackChanges);
	Task<IEnumerable<Notification>> GetByUserId(string userId, RequestParameters request, bool trackChanges);
	void CreateNotification(Notification notification);
	void UpdateNotification(Notification notification);
	void DeleteNotification(Notification notification);
}