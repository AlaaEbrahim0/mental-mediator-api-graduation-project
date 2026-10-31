using Application.Contracts;
using Domain.Entities;
using Infrastructure.Data;

namespace MentalMediator.Tests;

internal sealed class FakeNotificationSender : INotificationSender
{
	private readonly AppDbContext? _context;

	public FakeNotificationSender(AppDbContext? context = null)
	{
		_context = context;
	}

	public List<Notification> Sent { get; } = new();
	public List<Notification> Updated { get; } = new();
	public List<RoomMessageCall> RoomMessages { get; } = new();

	public Task SendNotificationAsync(Notification notification)
	{
		Sent.Add(notification);

		// Mirror the production sender's persist-before-deliver contract so
		// deduplication checks in the service see earlier deliveries.
		if (_context is not null)
		{
			_context.Notifications.Add(notification);
			_context.SaveChanges();
		}

		return Task.CompletedTask;
	}

	public Task SendRoomMessageNotificationAsync(
		string recipientUserId,
		int roomId,
		string roomName,
		string senderName,
		DateTime sentAtUtc)
	{
		RoomMessages.Add(new RoomMessageCall(recipientUserId, roomId, roomName, senderName, sentAtUtc));
		return Task.CompletedTask;
	}

	public Task NotifyUpdatedAsync(Notification notification)
	{
		Updated.Add(notification);
		return Task.CompletedTask;
	}
}

internal sealed record RoomMessageCall(
	string RecipientUserId,
	int RoomId,
	string RoomName,
	string SenderName,
	DateTime SentAtUtc);
