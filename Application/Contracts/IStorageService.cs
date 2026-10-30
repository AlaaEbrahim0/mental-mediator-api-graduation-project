using Microsoft.AspNetCore.Http;
using Shared;

namespace Application.Contracts;

public sealed record StoredMedia(string Url, string Type);

public interface IStorageService
{
	Task<Result<string>> UploadPhoto(IFormFile photo);

	/// <summary>Uploads an image or a short video and reports the stored URL and kind.</summary>
	Task<Result<StoredMedia>> UploadMedia(IFormFile file);

	/// <summary>
	/// True only for URLs under the configured Cloudinary cloud. Post media is
	/// uploaded first and referenced by URL afterwards, so the API must refuse
	/// arbitrary third-party URLs submitted alongside a post.
	/// </summary>
	bool IsManagedMediaUrl(string url);
}
