using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Reply
{
	public int Id { get; set; }
	public int CommentId { get; set; }
	public string AppUserId { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;

	/// <summary>Optional managed image/video attached when the reply is created.</summary>
	public string? MediaUrl { get; set; }

	/// <summary>"image" or "video", or null when the reply carries no media.</summary>
	public string? MediaType { get; set; }

	public DateTime RepliedAt { get; set; }

	public BaseUser AppUser { get; set; } = null!;
	public Comment Comment { get; set; } = null!;
	public List<ReplyReaction> Reactions { get; set; } = new();

	[NotMapped]
	public string? PhotoUrl { get; set; }

	[NotMapped]
	public string Username { get; set; } = null!;
}


