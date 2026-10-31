using Domain.Entities;

namespace Application.Contracts;

public interface INotificationSender
{
	Task SendNotificationAsync(Notification notification);

	/// <summary>
	/// Persists one aggregated notification per room and member, then pushes
	/// the resulting row. Repeated calls increment the unread count instead of
	/// creating one row per message.
	/// </summary>
	Task SendRoomMessageNotificationAsync(
		string recipientUserId,
		int roomId,
		string roomName,
		string senderName,
		DateTime sentAtUtc);

	/// <summary>Pushes a notification that changed without being re-created.</summary>
	Task NotifyUpdatedAsync(Notification notification);
}
