using Domain.Enums;

namespace Domain.Entities;

public class ChatRoomMember
{
	public int Id { get; set; }

	public int RoomId { get; set; }

	public ChatRoom Room { get; set; } = null!;

	public string UserId { get; set; } = string.Empty;

	public ChatRoomRole Role { get; set; } = ChatRoomRole.Member;

	public string DisplayName { get; set; } = string.Empty;

	public bool IsDoctor { get; set; }

	public DateTime JoinedAtUtc { get; set; }

	public DateTime? LeftAtUtc { get; set; }
}
