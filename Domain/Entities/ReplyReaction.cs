namespace Domain.Entities;

public class ReplyReaction
{
	public int Id { get; set; }

	public int ReplyId { get; set; }

	public Reply Reply { get; set; } = null!;

	public string UserId { get; set; } = string.Empty;

	public string Reaction { get; set; } = "like";

	public DateTime CreatedAtUtc { get; set; }
}
