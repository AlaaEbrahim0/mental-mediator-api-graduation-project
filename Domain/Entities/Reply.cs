using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Reply
{
	public int Id { get; set; }
	public int CommentId { get; set; }
	public string AppUserId { get; set; } = null!;
	public string Content { get; set; } = null!;
	public DateTime RepliedAt { get; set; }

	public BaseUser AppUser { get; set; } = null!;
	public Comment Comment { get; set; } = null!;

	[NotMapped]
	public string? PhotoUrl { get; set; }

	[NotMapped]
	public string Username { get; set; } = null!;
}


