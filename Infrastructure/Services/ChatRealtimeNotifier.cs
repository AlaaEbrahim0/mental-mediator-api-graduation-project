using Application.Contracts;
using Application.Dtos.ChatDtos;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ChatRealtimeNotifier : IChatRealtimeNotifier
{
	private readonly IHubContext<ChatHub, IChatClient> _hubContext;
	private readonly IChatConnectionRegistry _connections;
	private readonly ILogger<ChatRealtimeNotifier> _logger;

	public ChatRealtimeNotifier(
		IHubContext<ChatHub, IChatClient> hubContext,
		IChatConnectionRegistry connections,
		ILogger<ChatRealtimeNotifier> logger)
	{
		_hubContext = hubContext;
		_connections = connections;
		_logger = logger;
	}

	public Task MessageCreatedAsync(int roomId, ChatMessageResponse message) =>
		SendToRoomAsync(
			roomId,
			"MessageCreated",
			client => client.MessageCreated(message));

	public Task MessageDeletedAsync(int roomId, ChatMessageResponse message) =>
		SendToRoomAsync(
			roomId,
			"MessageDeleted",
			client => client.MessageDeleted(message));

	public Task MemberUpdatedAsync(int roomId, ChatRoomMemberResponse member) =>
		SendToRoomAsync(
			roomId,
			"MemberUpdated",
			client => client.MemberUpdated(member));

	public Task RoomUpdatedAsync(int roomId, ChatRoomResponse room) =>
		SendToRoomAsync(
			roomId,
			"RoomUpdated",
			client => client.RoomUpdated(room));

	public Task RoomArchivedAsync(int roomId, ChatRoomResponse room) =>
		SendToRoomAsync(
			roomId,
			"RoomArchived",
			client => client.RoomArchived(room));

	public async Task MemberLeftAsync(int roomId, int memberId, string userId)
	{
		// Remove the leaver's connections before broadcasting so they do not
		// receive their own leave event or any later room traffic.
		await RemoveUserFromRoomAsync(userId, roomId);

		await SendToRoomAsync(
			roomId,
			"MemberLeft",
			client => client.MemberLeft(memberId));
	}

	private async Task RemoveUserFromRoomAsync(string userId, int roomId)
	{
		foreach (var connectionId in _connections.GetConnections(userId))
		{
			if (!_connections.IsSubscribed(connectionId, roomId))
			{
				continue;
			}

			try
			{
				await _hubContext.Groups.RemoveFromGroupAsync(
					connectionId,
					ChatGroups.Room(roomId));

				_connections.RemoveSubscription(connectionId, roomId);
			}
			catch (Exception exception)
			{
				// Keep the registry entry on failure so a later disconnect or
				// leave attempt can still clean it up.
				_logger.LogError(
					exception,
					"Failed to remove a chat connection from room {RoomId}.",
					roomId);
			}
		}
	}

	private async Task SendToRoomAsync(
		int roomId,
		string eventName,
		Func<IChatClient, Task> send)
	{
		try
		{
			await send(_hubContext.Clients.Group(ChatGroups.Room(roomId)));
		}
		catch (Exception exception)
		{
			// The database change is already committed. A live delivery failure
			// must not fail the command; clients recover through the REST history
			// endpoints. Never log message content here.
			_logger.LogError(
				exception,
				"Failed to broadcast chat event {EventName} for room {RoomId}.",
				eventName,
				roomId);
		}
	}
}
