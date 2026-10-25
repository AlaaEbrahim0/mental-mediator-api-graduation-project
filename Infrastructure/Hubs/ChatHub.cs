using Application.Contracts;
using Application.Dtos.ChatDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
	private readonly IChatService _chatService;
	private readonly IChatConnectionRegistry _connections;
	private readonly ILogger<ChatHub> _logger;

	public ChatHub(
		IChatService chatService,
		IChatConnectionRegistry connections,
		ILogger<ChatHub> logger)
	{
		_chatService = chatService;
		_connections = connections;
		_logger = logger;
	}

	public override async Task OnConnectedAsync()
	{
		var userId = Context.User?.FindFirst("uid")?.Value;
		if (string.IsNullOrWhiteSpace(userId))
		{
			// [Authorize] proves the token is valid, but chat actions require the
			// account id. Reject a token that has no uid claim instead of letting
			// it hold a socket that can never authorize a room.
			_logger.LogWarning("Chat hub connection rejected because the token has no uid claim.");
			Context.Abort();
			return;
		}

		_connections.AddConnection(Context.ConnectionId, userId);
		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		_connections.RemoveConnection(Context.ConnectionId);
		await base.OnDisconnectedAsync(exception);
	}

	/// <summary>
	/// Subscribes the current connection to live events for a room. This does
	/// not create or reactivate membership; the user must already be an active
	/// member. The returned room snapshot lets a reconnecting client refresh
	/// archive state and its own member id/role.
	/// </summary>
	public async Task<ChatHubResult<ChatRoomResponse>> SubscribeToRoom(int roomId)
	{
		var initial = await _chatService.EnsureActiveMemberAsync(roomId);
		if (initial.IsFailure)
		{
			return ChatHubResult<ChatRoomResponse>.Fail(
				initial.Error.Code,
				initial.Error.Description);
		}

		// Register the subscription before joining the group so a concurrent
		// REST leave can find and remove this connection. Then recheck
		// membership after joining; this closes the race where a leave commits
		// between the first authorization check and the group add. The second
		// snapshot is returned so a reconnecting client sees current archive
		// state even if the room changed during the handshake.
		_connections.AddSubscription(Context.ConnectionId, roomId);

		try
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, ChatGroups.Room(roomId));
		}
		catch
		{
			_connections.RemoveSubscription(Context.ConnectionId, roomId);
			throw;
		}

		var verified = await _chatService.EnsureActiveMemberAsync(roomId);
		if (verified.IsFailure)
		{
			await RemoveSubscriptionAsync(roomId);
			return ChatHubResult<ChatRoomResponse>.Fail(
				verified.Error.Code,
				verified.Error.Description);
		}

		return ChatHubResult<ChatRoomResponse>.Ok(verified.Value);
	}

	/// <summary>
	/// Stops live delivery for a room on this connection. Membership is not
	/// changed; use the REST leave endpoint to leave the room.
	/// </summary>
	public async Task<ChatHubResult> UnsubscribeFromRoom(int roomId)
	{
		await RemoveSubscriptionAsync(roomId);
		return ChatHubResult.Ok();
	}

	private async Task RemoveSubscriptionAsync(int roomId)
	{
		try
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChatGroups.Room(roomId));
			_connections.RemoveSubscription(Context.ConnectionId, roomId);
		}
		catch (Exception exception)
		{
			// If the connection cannot leave the group, it could keep receiving
			// room events after a failed subscription check or an unsubscribe
			// request. Abort it instead: SignalR drops all of a connection's
			// group memberships on disconnect, and the client can reconnect and
			// re-subscribe only to rooms it is still authorized to read.
			_logger.LogError(
				exception,
				"Failed to remove a chat subscription for room {RoomId}; aborting the connection.",
				roomId);
			Context.Abort();
		}
	}
}
