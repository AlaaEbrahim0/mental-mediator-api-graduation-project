namespace Application.Dtos.PostsDto;

public class PostResponse
{
	public int Id { get; set; }

	public string? Title { get; set; }

	public string Content { get; set; } = string.Empty;
	public DateTime PostedOn { get; set; }

	public string? AppUserId { get; set; }

	public string Username { get; set; } = string.Empty;

	public string? PhotoUrl { get; set; }
	public bool IsAnonymous { get; set; }
}



