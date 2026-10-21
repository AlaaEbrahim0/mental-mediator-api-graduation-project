namespace Application.Dtos.PostsDto;

public class PostResponse
{
	public int Id { get; set; }

	public string? Title { get; set; }

	public string Content { get; set; } = string.Empty;
	public string? Category { get; set; }
	public string? Tags { get; set; }

	public DateTime PostedOn { get; set; }

	public string? AppUserId { get; set; }

	public string Username { get; set; } = string.Empty;

	public string? PhotoUrl { get; set; }

	public string? PostPhotoUrl { get; set; }

	public bool IsAnonymous { get; set; }

	public int CommentsCount { get; set; }
	public int LikesCount { get; set; }
	public bool IsLikedByCurrentUser { get; set; }

}



