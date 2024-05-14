using Application.Contracts;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using dotenv.net;
using Microsoft.AspNetCore.Http;
using Shared;

namespace Infrastructure.Services;
public class CloudinaryStorageService : IStorageService
{
	private readonly Cloudinary cloudinary;

	public CloudinaryStorageService()
	{
		DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
		cloudinary = new Cloudinary(Environment.GetEnvironmentVariable("CLOUDINARY_URL"));
		cloudinary.Api.Secure = true;
	}
	public async Task<Result<string>> UploadPhoto(IFormFile photo)
	{
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