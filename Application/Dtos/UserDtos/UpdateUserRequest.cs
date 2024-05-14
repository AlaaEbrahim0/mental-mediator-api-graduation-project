using Domain.Errors;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Dtos.UserDtos;
public class BaseUpdateUserInfoRequest
{
	public string FirstName { get; set; } = string.Empty;

	public string LastName { get; set; } = string.Empty;

	public DateOnly BirthDate { get; set; }

	public string Gender { get; set; } = string.Empty;

	public IFormFile? Photo { get; set; }
}

public class BaseUpdateUserInfoRequestValidator : AbstractValidator<BaseUpdateUserInfoRequest>
{
	private readonly List<string> allowedPhotoExtensions =
	[
		".png",
		".jpg",
		".jpeg",
	];
	private const long MaxPhotoSizeInBytes = 5 * 1024 * 1024;
	public BaseUpdateUserInfoRequestValidator()
	{
		RuleFor(x => x.FirstName)
			.NotNull()
			.Length(2, 64).WithMessage("First name must be between 2 and 64 characters");

		RuleFor(x => x.LastName)
			.NotNull()
			.Length(2, 64).WithMessage("Last name must be between 2 and 64 characters");

		RuleFor(x => x.BirthDate)
			.NotNull()
			.Must(x => x < DateOnly.FromDateTime(DateTime.Now))
			.WithMessage("Birthdate cannot be in the future");

		RuleFor(x => x.Gender)
			.NotNull()
			.Must(x => x!.Equals("male", StringComparison.InvariantCultureIgnoreCase) || x.Equals("female", StringComparison.InvariantCultureIgnoreCase))
			.WithMessage("Gender must be male or female");

		RuleFor(x => x.Photo)
			.Custom((photo, context) =>
			{
				var error = ValidatePhoto(photo);
				if (!string.IsNullOrEmpty(error))
				{
					context.AddFailure(error);
				}
			});

	}

	private string ValidatePhoto(IFormFile? photo)
	{
		if (photo == null)
		{
			return string.Empty;
		}

		var photoExtension = Path.GetExtension(photo.FileName);
		if (!allowedPhotoExtensions.Contains(photoExtension))
		{
			return StorageErrors.UnsupportedPhotoExtension(photoExtension).Description;
		}

		if (photo.Length > MaxPhotoSizeInBytes)
		{
			return StorageErrors.FileSizeExceededMaximumSize().Description;
		}

		return string.Empty;
	}
}

