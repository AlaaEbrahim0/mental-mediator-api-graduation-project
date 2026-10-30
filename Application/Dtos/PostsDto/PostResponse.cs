using Domain.Enums;

namespace Application.Dtos.PostsDto;

public class PostResponse
{
	public int Id { get; set; }

	public string? Title { get; set; }

	public string Content { get; set; } = string.Empty;
	public string? Category { get; set; }
	public string? Tags { get; set; }

	public DateTime PostedOn { get; set; }

	/// <summary>Null for anonymous posts; use <see cref="IsOwnPost"/> to manage your own.</summary>
	public string? AppUserId { get; set; }

	public string? Username { get; set; }

	public string? PhotoUrl { get; set; }

	/// <summary>First image URL. Kept for older clients; new clients render <see cref="Media"/>.</summary>
	public string? PostPhotoUrl { get; set; }

	public List<PostMediaResponse> Media { get; set; } = new();

	public PostVisibility Visibility { get; set; }

	/// <summary>The room whose active members can read a Specific Group post.</summary>
	public int? TargetGroupId { get; set; }

	public bool IsAnonymous { get; set; }

	public bool IsDraft { get; set; }

	public bool IsOwnPost { get; set; }

	public int CommentsCount { get; set; }
	public int LikesCount { get; set; }
	public bool IsLikedByCurrentUser { get; set; }

}
