namespace Domain.Entities;

/// <summary>
/// A file uploaded for a post before the post exists. Tracking the owner and
/// the attachment lets the API delete abandoned uploads without ever touching
/// media already attached to a published post.
/// </summary>
public class PostMediaAsset
{
	public int Id { get; set; }

	public string Url { get; set; } = string.Empty;

	/// <summary>Cloudinary public id used to destroy the asset.</summary>
	public string PublicId { get; set; } = string.Empty;

	public string MediaType { get; set; } = "image";

	public string OwnerId { get; set; } = string.Empty;

	public int? PostId { get; set; }

	public Post? Post { get; set; }

	/// <summary>Set when the asset is attached to a comment instead of a post.</summary>
	public int? CommentId { get; set; }

	public Comment? Comment { get; set; }

	/// <summary>Set when the asset is attached to a reply instead of a post.</summary>
	public int? ReplyId { get; set; }

	public Reply? Reply { get; set; }

	public DateTime CreatedAtUtc { get; set; }
}
