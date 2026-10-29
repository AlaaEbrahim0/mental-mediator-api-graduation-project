using Domain.Enums;

namespace Application.Dtos.ChatDtos;

public class ChatRoomResponse
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public ChatRoomVisibility Visibility { get; set; }

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? LastMessageAtUtc { get; set; }

	public bool IsArchived { get; set; }

	/// <summary>
	/// UI vocabulary for <see cref="Visibility"/>. Settable rather than computed so
	/// a SignalR payload deserializes into it; the server is the only writer.
	/// </summary>
	public bool IsPrivate { get; set; }

	/// <summary>
	/// True when joining needs a passphrase. The passphrase itself is never sent.
	/// </summary>
	public bool RequiresPassword { get; set; }

	/// <summary>A reversible pause: members read, nobody posts, nobody joins.</summary>
	public bool IsLocked { get; set; }

	public string? CoverImageUrl { get; set; }

	public DateTime? ScheduledStartAtUtc { get; set; }

	public int MemberCount { get; set; }

	public bool IsMember { get; set; }

	public ChatRoomRole? MyRole { get; set; }

	public int? MyMemberId { get; set; }

	/// <summary>
	/// Display name of the owner membership row, not the creator's account: the
	/// roster already stores a per-room snapshot, and a patient's snapshot is the
	/// pseudonymous alias, which is what should be shown on a card.
	/// </summary>
	public string? OwnerDisplayName { get; set; }

	public bool OwnerIsDoctor { get; set; }

	public string? LastMessagePreview { get; set; }
}

public class ChatRoomPageResponse
{
	public List<ChatRoomResponse> Rooms { get; set; } = new();

	public bool HasMore { get; set; }

	public int? NextBeforeRoomId { get; set; }
}

public class ChatRoomBrowseResponse
{
	public List<ChatRoomResponse> Rooms { get; set; } = new();

	public int TotalCount { get; set; }

	public int PageNumber { get; set; }

	public int PageSize { get; set; }

	public bool HasMore { get; set; }
}
