using Application.Contracts;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using dotenv.net;
using Microsoft.AspNetCore.Http;
using Shared;

namespace Infrastructure.Services;
public class CloudinaryStorageService : IStorageService
{
	private readonly string? cloudinaryUrl;
	private Cloudinary? cloudinary;

	public CloudinaryStorageService()
	{
		DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
		cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
	}
	public async Task<Result<string>> UploadPhoto(IFormFile photo)
	{
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

		var uploadParams = new ImageUploadParams()
		{
			File = new FileDescription()
			{
				FileName = Guid.NewGuid().ToString(),
				Stream = stream
			},
			Overwrite = true
		};

		ImageUploadResult uploadResult;
		uploadResult = await cloudinary.UploadAsync(uploadParams);
		if (uploadResult.Error != null)
		{
			return Shared.Error.ServiceUnavailable("ExternalServices.StorageServiceUnavailable", "failed to upload image");

		}

		return uploadResult.Url.ToString();
	}
}
