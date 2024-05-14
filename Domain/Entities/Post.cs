using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Post
{
	public int Id { get; set; }
	[Required(ErrorMessage = "AppUserId is required")]

	public string? AppUserId { get; set; }
	public BaseUser? AppUser { get; set; }

	[Required(ErrorMessage = "Title is required")]
	[MaxLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
	public string? Title { get; set; }

	[Required(ErrorMessage = "Content is required")]
	[MaxLength(2047, ErrorMessage = "Title cannot exceed 2048 characters")]
	public string Content { get; set; } = string.Empty;

	public bool IsAnonymous { get; set; }

	[NotMapped]
	public string? Username { get; set; }

	public DateTime PostedOn { get; set; }
	public List<Comment> Comments { get; set; } = new();
}
