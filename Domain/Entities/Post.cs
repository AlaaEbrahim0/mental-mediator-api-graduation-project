using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Post
{
	public int Id { get; set; }

	public string AppUserId { get; set; } = string.Empty;

	[NotMapped]
	public string? Username { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Content { get; set; } = string.Empty;

	public bool IsAnonymous { get; set; }

	[NotMapped]
	public string? PhotoUrl { get; set; }

	public BaseUser AppUser { get; set; } = null!;
	public DateTime PostedOn { get; set; }

	public string? PostPhotoUrl { get; set; }
	public List<Comment> Comments { get; set; } = new();

	[NotMapped]
	public int CommentsCount { get; set; }
}
