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

	Task RoomArchivedAsync(int roomId, ChatRoomResponse room);
}
