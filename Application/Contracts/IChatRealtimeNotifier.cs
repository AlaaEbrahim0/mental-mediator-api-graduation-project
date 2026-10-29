using Application.Dtos.ChatDtos;

namespace Application.Contracts;

/// <summary>
/// Publishes chat events after the corresponding database change has been
/// persisted. Implementations must not throw into the command path: a live
/// delivery failure is recoverable from the REST history endpoints.
/// </summary>
public interface IChatRealtimeNotifier
{
	Task MessageCreatedAsync(int roomId, ChatMessageResponse message);

	Task MessageDeletedAsync(int roomId, ChatMessageResponse message);

	Task MemberUpdatedAsync(int roomId, ChatRoomMemberResponse member);

	Task MemberLeftAsync(int roomId, int memberId, string userId);

	/// <summary>
	/// Broadcasts a room snapshot after a metadata change: lock, archive, privacy,
	/// passphrase, cover or schedule. Clients merge only the fields they may show
	/// for a non-member and never overwrite their own membership state.
	/// </summary>
	Task RoomUpdatedAsync(int roomId, ChatRoomResponse room);

	Task RoomArchivedAsync(int roomId, ChatRoomResponse room);
}
