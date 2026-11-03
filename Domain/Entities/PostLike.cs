using Domain.Enums;

namespace Domain.Entities;

public class PostLike
{
	public int Id { get; set; }
	public int PostId { get; set; }
	public string UserId { get; set; } = string.Empty;

	/// <summary>
	/// The reaction token ("like", "love", "haha", "wow" or "sad"). The table
	/// keeps its historical name so existing like endpoints and rows stay valid.
	/// </summary>
	public string Reaction { get; set; } = ReactionKinds.Like;

	public DateTime LikedOn { get; set; }
	public Post Post { get; set; } = null!;
}
