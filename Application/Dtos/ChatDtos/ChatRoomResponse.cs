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

	public int MemberCount { get; set; }

	public bool IsMember { get; set; }

	public ChatRoomRole? MyRole { get; set; }

	public int? MyMemberId { get; set; }

	public string? LastMessagePreview { get; set; }
}

public class ChatRoomPageResponse
{
	public List<ChatRoomResponse> Rooms { get; set; } = new();

	public bool HasMore { get; set; }

	public int? NextBeforeRoomId { get; set; }
}
