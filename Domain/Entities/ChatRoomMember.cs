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

	/// <summary>
	/// Instant up to which the member has read the room. NULL means "nothing
	/// read yet", so the count falls back to <see cref="JoinedAtUtc"/>.
	/// </summary>
	public DateTime? LastReadAtUtc { get; set; }

	public DateTime? LeftAtUtc { get; set; }
}
