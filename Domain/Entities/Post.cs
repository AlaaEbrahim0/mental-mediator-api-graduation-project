using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

public class Post
{
	public int Id { get; set; }

	public string AppUserId { get; set; } = string.Empty;


	[NotMapped]
	public string? Username { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Content { get; set; } = string.Empty;
	public string? Category { get; set; }
	public string? Tags { get; set; }

	public bool IsAnonymous { get; set; }

	public PostVisibility Visibility { get; set; } = PostVisibility.Public;

	/// <summary>
	/// The room whose active members may read a Specific Group post. Reusing the
	/// existing room membership model avoids inventing a second group system.
	/// Null for the Public and DoctorsOnly audiences.
	/// </summary>
	public int? TargetChatRoomId { get; set; }

	public ChatRoom? TargetChatRoom { get; set; }

	/// <summary>
	/// Drafts are visible only to their owner and administrators. They are
	/// excluded from every feed and cannot be liked or commented on.
	/// </summary>
	public bool IsDraft { get; set; }

	/// <summary>JSON array of uploaded media: [{ "url": "...", "type": "image" }].</summary>
	public string? MediaJson { get; set; }

	[NotMapped]
	public string? PhotoUrl { get; set; }

	public BaseUser AppUser { get; set; } = null!;
	public DateTime PostedOn { get; set; }

	public string? PostPhotoUrl { get; set; }
	public List<Comment> Comments { get; set; } = new();
	public List<PostLike> Likes { get; set; } = new();

	[NotMapped]
	public int CommentsCount { get; set; }
	[NotMapped]
	public int LikesCount { get; set; }
	[NotMapped]
	public bool IsLikedByCurrentUser { get; set; }
}
