﻿using Application.Contracts;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Domain.Errors;
using dotenv.net;
using Microsoft.AspNetCore.Http;
using Shared;

namespace Infrastructure.Services;
public class StorageService : IStorageService
{
    private readonly Cloudinary cloudinary;
    private readonly List<string> allowedPhotoExtensions =
    [
        ".png",
        ".jpg",
        ".jpeg",
    ];
    private const long MaxPhotoSizeInBytes = 5 * 1024 * 1024;

    public StorageService()
    {
        DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
        cloudinary = new Cloudinary(Environment.GetEnvironmentVariable("CLOUDINARY_URL"));
        cloudinary.Api.Secure = true;
    }
    public async Task<Result<string>> UploadPhoto(IFormFile photo)
    {
        var photoExtension = Path.GetExtension(photo.FileName);
        if (!allowedPhotoExtensions.Contains(photoExtension))
        {
            return StorageErrors.UnsupportedPhotoExtension(photoExtension);
        }

        if (photo.Length > MaxPhotoSizeInBytes)
        {
            return StorageErrors.FileSizeExceededMaximumSize();
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

        var uploadResult = await cloudinary.UploadAsync(uploadParams);

        return uploadResult.Url.ToString();
    }
}