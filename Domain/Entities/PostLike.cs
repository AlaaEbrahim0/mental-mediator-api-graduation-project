namespace Domain.Entities;

public class PostLike
{
	public int Id { get; set; }
	public int PostId { get; set; }
	public string UserId { get; set; } = string.Empty;
	public DateTime LikedOn { get; set; }
	public Post Post { get; set; } = null!;
}
