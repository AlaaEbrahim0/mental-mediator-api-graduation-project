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
	private const long MaxAudioBytes = 10 * 1024 * 1024;
	private const string UploadFolder = "posts";

	private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
	private static readonly string[] VideoExtensions = { ".mp4", ".webm", ".mov" };

	// Voice notes are recorded by the browser as audio/webm, audio/ogg or
	// audio/mp4. The matching extension is checked as a fallback for clients
	// that send a generic content type.
	private static readonly string[] AudioExtensions = { ".mp3", ".m4a", ".wav", ".ogg", ".oga", ".aac", ".opus", ".weba", ".flac" };

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

		var mediaKind = DetectMediaKind(file);
		if (mediaKind == MediaKind.None)
		{
			return Shared.Error.Validation(
				"Storage.UnsupportedMedia",
				"Only JPG, JPEG, PNG, GIF, WebP images, MP4, WebM, MOV videos and MP3, M4A, WAV, OGG, AAC audio files can be uploaded.");
		}

		if (imagesOnly && mediaKind != MediaKind.Image)
		{
			return Shared.Error.Validation(
				"Storage.NotAnImage",
				"Only JPG, JPEG, PNG, GIF, and WebP images can be uploaded.");
		}

		var maxBytes = mediaKind switch
		{
			MediaKind.Audio => MaxAudioBytes,
			MediaKind.Video => MaxVideoBytes,
			_ => MaxImageBytes
		};

		if (file.Length > maxBytes)
		{
			var label = mediaKind switch
			{
				MediaKind.Audio => "Audio",
				MediaKind.Video => "Videos",
				_ => "Images"
			};
			return Shared.Error.Validation(
				"Storage.FileTooLarge",
				$"{label} must be smaller than {maxBytes / (1024 * 1024)} MB.");
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
			if (mediaKind is MediaKind.Video or MediaKind.Audio)
			{
				// Cloudinary stores audio under its video resource type.
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

		// SecureUrl is normally populated, but the fallback must never put a
		// plain-HTTP URL into a post. Only Cloudinary URLs are rewritten.
		url = Application.Dtos.PostsDto.PostMediaCodec.NormalizeSecureUrl(url);

		return new StoredMedia(
			url,
			mediaKind switch
			{
				MediaKind.Video => "video",
				MediaKind.Audio => "audio",
				_ => "image"
			},
			uploadResult.PublicId ?? $"{UploadFolder}/{publicId}");
	}

	public async Task<Result> DeleteMediaAsync(string publicId, string mediaType)
	{
		if (string.IsNullOrWhiteSpace(publicId))
		{
			return Result.Success();
		}

		var client = GetClient();
		if (client is null)
		{
			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"Uploads require a valid CLOUDINARY_URL (cloudinary://<api_key>:<api_secret>@<cloud_name>).");
		}

		try
		{
			var result = await client.DestroyAsync(new DeletionParams(publicId)
			{
				ResourceType = string.Equals(mediaType, "video", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(mediaType, "audio", StringComparison.OrdinalIgnoreCase)
					? ResourceType.Video
					: ResourceType.Image
			});

			// "not found" is success for cleanup: the desired end state (no asset)
			// already holds, for example after a manual provider-side deletion.
			if (string.Equals(result.Result, "ok", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(result.Result, "not found", StringComparison.OrdinalIgnoreCase))
			{
				return Result.Success();
			}

			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"The storage provider could not delete the media.");
		}
		catch
		{
			// Provider exceptions can include account identifiers; keep them out
			// of the client payload and retry on the next cleanup pass.
			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"The storage provider could not delete the media.");
		}
	}

	private enum MediaKind
	{
		None,
		Image,
		Video,
		Audio
	}

	private static MediaKind DetectMediaKind(IFormFile file)
	{
		var contentType = file.ContentType ?? string.Empty;
		var extension = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();

		// Audio is checked first because a browser voice note can be
		// audio/webm, which shares the .webm extension with video.
		if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
			AudioExtensions.Contains(extension))
		{
			return MediaKind.Audio;
		}

		if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
			VideoExtensions.Contains(extension))
		{
			return MediaKind.Video;
		}

		if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
			ImageExtensions.Contains(extension))
		{
			return MediaKind.Image;
		}

		return MediaKind.None;
	}

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
