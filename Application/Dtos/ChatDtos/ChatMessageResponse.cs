namespace Application.Dtos.ChatDtos;

public class ChatMessageResponse
{
	public long Id { get; set; }

	public int RoomId { get; set; }

	public int SenderMemberId { get; set; }

	/// <summary>
	/// The sender's idempotency key. It is a random GUID, not a user id, and
	/// lets a client reconcile its optimistic message with the persisted one.
	/// </summary>
	public Guid ClientMessageId { get; set; }

	public string SenderDisplayName { get; set; } = string.Empty;

	public bool SenderIsDoctor { get; set; }

	public string Content { get; set; } = string.Empty;

	public DateTime SentAtUtc { get; set; }

	public bool IsDeleted { get; set; }
}
