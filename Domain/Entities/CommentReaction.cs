namespace Domain.Entities;

public class CommentReaction
{
	public int Id { get; set; }

	public int CommentId { get; set; }

	public Comment Comment { get; set; } = null!;

	public string UserId { get; set; } = string.Empty;

	public string Reaction { get; set; } = "like";

	public DateTime CreatedAtUtc { get; set; }
}
