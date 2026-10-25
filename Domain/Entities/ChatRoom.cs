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

	public List<ChatRoomMember> Members { get; set; } = new();

	public List<ChatMessage> Messages { get; set; } = new();
}
