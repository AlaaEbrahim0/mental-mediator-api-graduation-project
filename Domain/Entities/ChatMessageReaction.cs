namespace Domain.Entities;

public class ChatMessageReaction
{
	public long Id { get; set; }

	public long MessageId { get; set; }

	public ChatMessage Message { get; set; } = null!;

	public string UserId { get; set; } = string.Empty;

	public string Reaction { get; set; } = "like";

	public DateTime CreatedAtUtc { get; set; }
}
