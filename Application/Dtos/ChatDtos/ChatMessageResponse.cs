using Application.Dtos.ReactionsDto;

namespace Application.Dtos.ChatDtos;

public class ChatMessageResponse
{
	public long Id { get; set; }

	public int RoomId { get; set; }

	public int SenderMemberId { get; set; }

	/// <summary>The sender's account id, used to open their profile.</summary>
	public string SenderUserId { get; set; } = string.Empty;

	/// <summary>
	/// The sender's idempotency key. It is a random GUID, not a user id, and
	/// lets a client reconcile its optimistic message with the persisted one.
	/// </summary>
	public Guid ClientMessageId { get; set; }

	public string SenderDisplayName { get; set; } = string.Empty;

	public bool SenderIsDoctor { get; set; }

	public string Content { get; set; } = string.Empty;

	/// <summary>Managed URL of an attached image/video, or null. Hidden when the message is deleted.</summary>
	public string? MediaUrl { get; set; }

	/// <summary>"image", "video" or "audio", or null.</summary>
	public string? MediaType { get; set; }

	public DateTime SentAtUtc { get; set; }

	public bool IsDeleted { get; set; }

	public List<ReactionSummaryResponse> Reactions { get; set; } = new();
	public string? MyReaction { get; set; }
}
