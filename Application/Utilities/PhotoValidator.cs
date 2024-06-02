using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Utilities;

public class PhotoValidator : AbstractValidator<IFormFile>
{
    private readonly string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private readonly long maxSize = 5 * 1024 * 1024;
    public PhotoValidator()
    {
        RuleFor(x => x.Length)
            .LessThan(maxSize)
            .WithMessage($"File size should be less than {maxSize / (1024 * 1024)} MB");

        RuleFor(x => x.FileName)
            .Must(IsValidFileExtension)
            .WithMessage("Only JPG, JPEG, PNG, and GIF files are allowed.");
    }

    private bool IsValidFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return !string.IsNullOrEmpty(extension) && allowedExtensions.Contains(extension);
    }
}