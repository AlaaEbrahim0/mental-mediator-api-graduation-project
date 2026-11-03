using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;
public class Comment
{
	public int Id { get; set; }
	public int PostId { get; set; }
	public string AppUserId { get; set; } = string.Empty;
	public Post Post { get; set; } = null!;
	public BaseUser AppUser { get; set; } = null!;

	public string Content { get; set; } = string.Empty;

	/// <summary>Optional managed image/video attached when the comment is created.</summary>
	public string? MediaUrl { get; set; }

	/// <summary>"image" or "video", or null when the comment carries no media.</summary>
	public string? MediaType { get; set; }

	public DateTime CommentedAt { get; set; }

	public List<Reply> Replies { get; set; } = new();
	public List<CommentReaction> Reactions { get; set; } = new();

	[NotMapped]
	public string? PhotoUrl { get; set; }

	[NotMapped]
	public string? Username { get; set; }

	[NotMapped]
	public int RepliesCount { get; set; }

}

