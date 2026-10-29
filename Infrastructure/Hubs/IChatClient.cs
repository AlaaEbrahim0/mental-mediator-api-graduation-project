using Application.Dtos.ChatDtos;

namespace Infrastructure.Hubs;

public interface IChatClient
{
	Task MessageCreated(ChatMessageResponse message);

	Task MessageDeleted(ChatMessageResponse message);

	Task MemberUpdated(ChatRoomMemberResponse member);

	Task MemberLeft(int memberId);

	Task RoomUpdated(ChatRoomResponse room);

	Task RoomArchived(ChatRoomResponse room);
}
