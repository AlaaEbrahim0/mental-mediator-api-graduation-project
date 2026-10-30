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

	public DateTime CreatedAtUtc { get; set; }
}
