using Domain.Entities;
using Shared.RequestParameters;

namespace Domain.Repositories
{
	public interface INotificationRepository
	{
		Task<Notification?> GetById(int notificationId, bool trackChanges);
		Task<IEnumerable<Notification>> GetByUserId(string userId, RequestParameters request, bool trackChanges);
		void CreateNotification(Notification notification);
		Task MarkAllAsRead(string userId);
		void UpdateNotification(Notification notification);
		Task<int> GetUnreadCountAsync(string userId);
		Task<bool> MarkMultipleAsReadAsync(IEnumerable<int> ids, string userId);
		Task<bool> ExistsByDeduplicationKeyAsync(string userId, string deduplicationKey);
		Task<Notification> UpsertRoomMessageNotificationAsync(
			string userId,
			int roomId,
			string roomName,
			string senderName,
			DateTime now);
		Task<IReadOnlyList<Notification>> MarkRoomNotificationsAsReadAsync(
			string userId,
			int roomId,
			DateTimeOffset readAt);
	}
}
