using Domain.Enums;

namespace Domain.Entities;

public class ChatRoom
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public ChatRoomVisibility Visibility { get; set; } = ChatRoomVisibility.Public;

	public string CreatedById { get; set; } = string.Empty;

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? LastMessageAtUtc { get; set; }

	public bool IsArchived { get; set; }

	/// <summary>
	/// Derived form of <see cref="Visibility"/>: NULL means the room is open to
	/// anyone who can see it. Kept separate rather than folded into the enum
	/// because a private room with no passphrase is still a members-only room.
	/// Never sent to a client.
	/// </summary>
	public string? PasswordHash { get; set; }

	public string? CoverImageUrl { get; set; }

	/// <summary>
	/// A reversible pause: members can read, nobody can post. Distinct from
	/// <see cref="IsArchived"/>, which closes the room and removes it from the
	/// directory.
	/// </summary>
	public bool IsLocked { get; set; }

	/// <summary>Optional future start, used by the dashboard's upcoming-rooms card.</summary>
	public DateTime? ScheduledStartAtUtc { get; set; }

	public List<ChatRoomMember> Members { get; set; } = new();

	public List<ChatMessage> Messages { get; set; } = new();
}
