using Application.Contracts;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using dotenv.net;
using Microsoft.AspNetCore.Http;
using Shared;

namespace Infrastructure.Services;
public class CloudinaryStorageService : IStorageService
{
	// Mirrors PhotoValidator's 5 MB cap. Enforced here as well because not every
	// upload path is behind that validator (UpdatePostRequest has none).
	private const long MaxImageBytes = 5 * 1024 * 1024;
	private const long MaxVideoBytes = 20 * 1024 * 1024;
	private const string UploadFolder = "posts";

	private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
	private static readonly string[] VideoExtensions = { ".mp4", ".webm", ".mov" };

	private readonly string? cloudinaryUrl;
	private Cloudinary? cloudinary;

	public CloudinaryStorageService()
	{
		// Same guard as Program.cs: probing walks up the directory tree looking
		// for a .env, which throws UnauthorizedAccessException on shared IIS
		// hosting where an ancestor directory is unreadable by the app pool.
		// CLOUDINARY_URL is supplied by the host environment in production.
		if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
		                   "Development", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
			}
			catch
			{
				// Fall back to the process environment.
			}
		}

		cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
	}

	public async Task<Result<string>> UploadPhoto(IFormFile photo)
	{
		var result = await Upload(photo, imagesOnly: true);
		return result.IsFailure ? result.Error : result.Value.Url;
	}

	public Task<Result<StoredMedia>> UploadMedia(IFormFile file) => Upload(file, imagesOnly: false);

	public bool IsManagedMediaUrl(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
			!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
			!string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var cloudName = GetCloudName();
		return cloudName is not null &&
		       uri.AbsolutePath.StartsWith($"/{cloudName}/", StringComparison.OrdinalIgnoreCase);
	}

	private async Task<Result<StoredMedia>> Upload(IFormFile? file, bool imagesOnly)
	{
		if (file is null || file.Length == 0)
		{
			return Shared.Error.Validation(
				"Storage.NoFile",
				"No file was supplied. Send it as multipart form data.");
		}

		var isVideo = IsVideo(file);
		if (isVideo && imagesOnly)
		{
			return Shared.Error.Validation(
				"Storage.NotAnImage",
				"Only JPG, JPEG, PNG, GIF, and WebP images can be uploaded.");
		}

		if (!isVideo && !IsImage(file))
		{
			return Shared.Error.Validation(
				"Storage.UnsupportedMedia",
				"Only JPG, JPEG, PNG, GIF, WebP images and MP4, WebM, MOV videos can be uploaded.");
		}

		var maxBytes = isVideo ? MaxVideoBytes : MaxImageBytes;
		if (file.Length > maxBytes)
		{
			return Shared.Error.Validation(
				"Storage.FileTooLarge",
				$"{(isVideo ? "Videos" : "Images")} must be smaller than {maxBytes / (1024 * 1024)} MB.");
		}

		var client = GetClient();
		if (client is null)
		{
			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"Uploads require a valid CLOUDINARY_URL (cloudinary://<api_key>:<api_secret>@<cloud_name>).");
		}

		using var stream = file.OpenReadStream();

		// The extension is preserved so Cloudinary records the stored format on
		// the delivered URL instead of relying solely on content sniffing. The
		// original name is never reused: it reaches the public CDN URL.
		var publicId = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";

		UploadResult uploadResult;
		try
		{
			var description = new FileDescription { FileName = publicId, Stream = stream };
			if (isVideo)
			{
				uploadResult = await client.UploadAsync(new VideoUploadParams
				{
					File = description,
					PublicId = publicId,
					Overwrite = true,
					Folder = UploadFolder
				});
			}
			else
			{
				uploadResult = await client.UploadAsync(new ImageUploadParams
				{
					File = description,
					PublicId = publicId,
					Overwrite = true,
					Folder = UploadFolder
				});
			}
		}
		catch
		{
			// Provider exceptions can include account identifiers; keep them out
			// of the client payload.
			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"failed to upload file");
		}

		if (uploadResult.Error != null)
		{
			// uploadResult.Error carries the provider response, which can include
			// account identifiers; keep it out of the log and the client payload.
			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"failed to upload file");
		}

		var url = (uploadResult.SecureUrl ?? uploadResult.Url)?.ToString();
		if (string.IsNullOrWhiteSpace(url))
		{
			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"The storage provider accepted the upload but returned no URL.");
		}

		return new StoredMedia(url, isVideo ? "video" : "image");
	}

	private static bool IsVideo(IFormFile file) =>
		(file.ContentType ?? string.Empty).StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
		VideoExtensions.Contains(Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant());

	private static bool IsImage(IFormFile file) =>
		(file.ContentType ?? string.Empty).StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
		ImageExtensions.Contains(Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant());

	private Cloudinary? GetClient()
	{
		if (string.IsNullOrWhiteSpace(cloudinaryUrl) ||
			!cloudinaryUrl.StartsWith("cloudinary://", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		try
		{
			cloudinary ??= new Cloudinary(cloudinaryUrl);
			cloudinary.Api.Secure = true;
			return cloudinary;
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	private string? GetCloudName()
	{
		if (string.IsNullOrWhiteSpace(cloudinaryUrl))
		{
			return null;
		}

		// CLOUDINARY_URL is cloudinary://<api_key>:<api_secret>@<cloud_name>.
		var at = cloudinaryUrl.LastIndexOf('@');
		if (at < 0 || at == cloudinaryUrl.Length - 1)
		{
			return null;
		}

		var cloudName = cloudinaryUrl[(at + 1)..].TrimEnd('/');
		return cloudName.Length == 0 || cloudName.Contains('/') || cloudName.Contains('?')
			? null
			: cloudName;
	}
}
