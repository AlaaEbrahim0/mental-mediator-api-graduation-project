namespace Domain.Entities;

public class ChatMessage
{
	public long Id { get; set; }

	public int RoomId { get; set; }

	public ChatRoom Room { get; set; } = null!;

	public string SenderId { get; set; } = string.Empty;

	// Immutable per-message snapshot. Room membership can change after a
	// message is sent, but history must keep showing the identity that was
	// visible at send time.
	public int SenderMemberId { get; set; }

	public string SenderDisplayName { get; set; } = string.Empty;

	public bool SenderIsDoctor { get; set; }

	public string Content { get; set; } = string.Empty;

	/// <summary>
	/// Optional image or video attached to the message. The URL always points at
	/// a managed storage asset: the send path rejects any URL the storage service
	/// did not itself issue, so a client cannot smuggle in a third-party link.
	/// </summary>
	public string? MediaUrl { get; set; }

	/// <summary>"image" or "video", or null when the message carries no media.</summary>
	public string? MediaType { get; set; }

	public Guid ClientMessageId { get; set; }

	public DateTime SentAtUtc { get; set; }

	public bool IsDeleted { get; set; }

	public DateTime? DeletedAtUtc { get; set; }

	public string? DeletedById { get; set; }

	public List<ChatMessageReaction> Reactions { get; set; } = new();
}
