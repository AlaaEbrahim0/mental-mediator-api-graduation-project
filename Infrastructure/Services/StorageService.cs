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
	private const long MaxUploadBytes = 5 * 1024 * 1024;

	private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

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
		var validationError = Validate(photo);
		if (validationError is not null)
		{
			return validationError;
		}

		var url = cloudinaryUrl;
		if (string.IsNullOrWhiteSpace(url) ||
			!url.StartsWith("cloudinary://", StringComparison.OrdinalIgnoreCase))
		{
			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"Image uploads require a valid CLOUDINARY_URL (cloudinary://<api_key>:<api_secret>@<cloud_name>).");
		}

		try
		{
			cloudinary ??= new Cloudinary(url);
			cloudinary.Api.Secure = true;
		}
		catch (ArgumentException)
		{
			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"CLOUDINARY_URL is malformed. Use cloudinary://<api_key>:<api_secret>@<cloud_name>.");
		}

		using var stream = photo.OpenReadStream();

		// The extension is preserved so Cloudinary records the stored format on
		// the delivered URL instead of relying solely on content sniffing. The
		// original name is never reused: it reaches the public CDN URL.
		var publicId = $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName).ToLowerInvariant()}";

		var uploadParams = new ImageUploadParams()
		{
			File = new FileDescription()
			{
				FileName = publicId,
				Stream = stream
			},
			PublicId = publicId,
			Overwrite = true
		};

		ImageUploadResult uploadResult;
		uploadResult = await cloudinary.UploadAsync(uploadParams);
		if (uploadResult.Error != null)
		{
			// uploadResult.Error carries the provider response, which can include
			// account identifiers; keep it out of the log and the client payload.
			return Shared.Error.ServiceUnavailable("ExternalServices.StorageServiceUnavailable", "failed to upload image");

		}

		if (string.IsNullOrWhiteSpace(uploadResult.Url?.ToString()))
		{
			return Shared.Error.ServiceUnavailable(
				"ExternalServices.StorageServiceUnavailable",
				"The storage provider accepted the upload but returned no URL.");
		}

		return uploadResult.Url.ToString();
	}

	private static Shared.Error? Validate(IFormFile? photo)
	{
		if (photo is null || photo.Length == 0)
		{
			return Shared.Error.Validation(
				"Storage.NoFile",
				"No image was supplied. Send the file as multipart form data.");
		}

		if (photo.Length > MaxUploadBytes)
		{
			return Shared.Error.Validation(
				"Storage.FileTooLarge",
				$"Images must be smaller than {MaxUploadBytes / (1024 * 1024)} MB.");
		}

		// Content type comes from the client, so the extension is consulted too
		// before rejecting: some clients submit a correct image with a generic
		// or empty content type, and those uploads used to succeed.
		var contentType = photo.ContentType ?? string.Empty;
		var extension = Path.GetExtension(photo.FileName ?? string.Empty).ToLowerInvariant();

		var looksLikeImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
		                     || AllowedExtensions.Contains(extension);

		if (!looksLikeImage)
		{
			return Shared.Error.Validation(
				"Storage.NotAnImage",
				"Only JPG, JPEG, PNG, GIF, and WebP images can be uploaded.");
		}

		return null;
	}
}
