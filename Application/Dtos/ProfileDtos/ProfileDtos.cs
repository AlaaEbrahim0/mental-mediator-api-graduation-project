using Application.Utilities;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Dtos.ProfileDtos;

public class UpdateProfileRequest
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public DateOnly BirthDate { get; set; }
	public string Gender { get; set; } = string.Empty;
	public string? Biography { get; set; }
	public string? PhoneNumber { get; set; }
	public string? City { get; set; }
	public string? ContactEmail { get; set; }
	public string? ContactPhone { get; set; }
	public string? ContactAddress { get; set; }

	public string? Headline { get; set; }
	public string? Pronouns { get; set; }
	public string? CoverPhotoUrl { get; set; }
	public string? WebsiteUrl { get; set; }
	public string? LinkedInUrl { get; set; }
	public string? TwitterUrl { get; set; }
	public string? InstagramUrl { get; set; }
	public string? FacebookUrl { get; set; }
	public string? YouTubeUrl { get; set; }

	public List<string> Languages { get; set; } = new();
	public List<string> Interests { get; set; } = new();

	/// <summary>
	/// Leave null to keep the current visibility. New doctor accounts are
	/// created public; new patient accounts are created private.
	/// </summary>
	public bool? IsProfilePublic { get; set; }

	/// <summary>Only applied when the current account is a doctor.</summary>
	public UpdateDoctorProfileRequest? Doctor { get; set; }
}

public class UpdateDoctorProfileRequest
{
	public string? Location { get; set; }
	public string? ProfessionalTitle { get; set; }
	public string? ClinicName { get; set; }
	public string? Qualifications { get; set; }
	public int? YearsOfExperience { get; set; }
	public decimal SessionFees { get; set; }
	public DoctorSpecialization Specialization { get; set; }
	public bool IsAvailable { get; set; } = true;
}

public class UpdateProfilePhotoRequest
{
	public IFormFile Photo { get; set; } = null!;
}

public class CertificateRequest
{
	public string Title { get; set; } = string.Empty;
	public string IssuingOrganization { get; set; } = string.Empty;
	public DateOnly IssueDate { get; set; }
	public DateOnly? ExpiryDate { get; set; }
	public string? CredentialId { get; set; }
	public string? CredentialUrl { get; set; }
	public string? Description { get; set; }
	public IFormFile? Image { get; set; }
}

public class QualificationRequest
{
	public string Degree { get; set; } = string.Empty;
	public string Institution { get; set; } = string.Empty;
	public string? FieldOfStudy { get; set; }
	public int? StartYear { get; set; }
	public int? EndYear { get; set; }
	public string? Description { get; set; }
}

public class SocialLinksResponse
{
	public string? WebsiteUrl { get; set; }
	public string? LinkedInUrl { get; set; }
	public string? TwitterUrl { get; set; }
	public string? InstagramUrl { get; set; }
	public string? FacebookUrl { get; set; }
	public string? YouTubeUrl { get; set; }
}

public class ContactInfoResponse
{
	public string? Email { get; set; }
	public string? Phone { get; set; }
	public string? Address { get; set; }
}

public class CertificateResponse
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string IssuingOrganization { get; set; } = string.Empty;
	public DateOnly IssueDate { get; set; }
	public DateOnly? ExpiryDate { get; set; }
	public bool IsExpired { get; set; }
	public string? CredentialId { get; set; }
	public string? CredentialUrl { get; set; }
	public string? Description { get; set; }
	public string? ImageUrl { get; set; }
}

public class QualificationResponse
{
	public int Id { get; set; }
	public string Degree { get; set; } = string.Empty;
	public string Institution { get; set; } = string.Empty;
	public string? FieldOfStudy { get; set; }
	public int? StartYear { get; set; }
	public int? EndYear { get; set; }
	public string? Description { get; set; }
}

public class DoctorProfileResponse
{
	public DoctorSpecialization Specialization { get; set; }
	public string? ProfessionalTitle { get; set; }
	public string? ClinicName { get; set; }
	public string? Location { get; set; }
	public string? Qualifications { get; set; }
	public int? YearsOfExperience { get; set; }
	public decimal SessionFees { get; set; }
	public bool IsAvailable { get; set; }
	public int CompletedAppointments { get; set; }
}

public class PublicProfileResponse
{
	public string Id { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
	public string? PhotoUrl { get; set; }
	public string? CoverPhotoUrl { get; set; }
	public string? Headline { get; set; }
	public string? Pronouns { get; set; }
	public string? Biography { get; set; }
	public string? City { get; set; }
	public List<string> Languages { get; set; } = new();
	public List<string> Interests { get; set; } = new();
	public bool IsProfilePublic { get; set; }
	public SocialLinksResponse SocialLinks { get; set; } = new();
	public ContactInfoResponse Contact { get; set; } = new();
	public List<CertificateResponse> Certificates { get; set; } = new();
	public List<QualificationResponse> Qualifications { get; set; } = new();
	public DoctorProfileResponse? Doctor { get; set; }
}

public class MyProfileResponse : PublicProfileResponse
{
	public string Email { get; set; } = string.Empty;
	public DateOnly BirthDate { get; set; }
	public string Gender { get; set; } = string.Empty;
	public string? PhoneNumber { get; set; }
}

internal static class ProfileValidation
{
	public static bool BeAValidHttpUrl(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return true;
		}

		return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
			(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
	}

	public static bool BeAValidName(string? value) =>
		!string.IsNullOrWhiteSpace(value) && value.Trim().Length is >= 2 and <= 64;

	public static bool BeAValidGender(string? value) =>
		!string.IsNullOrWhiteSpace(value) &&
		(value.Equals("male", StringComparison.OrdinalIgnoreCase) ||
		 value.Equals("female", StringComparison.OrdinalIgnoreCase));
}

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
	public UpdateProfileRequestValidator()
	{
		RuleFor(request => request.FirstName)
			.Must(ProfileValidation.BeAValidName)
			.WithMessage("First name must be between 2 and 64 characters.");

		RuleFor(request => request.LastName)
			.Must(ProfileValidation.BeAValidName)
			.WithMessage("Last name must be between 2 and 64 characters.");

		RuleFor(request => request.BirthDate)
			.NotEmpty()
			.WithMessage("Birth date is required.")
			.Must(birthDate => birthDate < DateOnly.FromDateTime(DateTime.UtcNow))
			.WithMessage("Birth date cannot be in the future.");

		RuleFor(request => request.Gender)
			.Must(ProfileValidation.BeAValidGender)
			.WithMessage("Gender must be male or female.");

		RuleFor(request => request.Biography).MaximumLength(1000);
		RuleFor(request => request.PhoneNumber).MaximumLength(32);
		RuleFor(request => request.City).MaximumLength(100);
		RuleFor(request => request.Headline).MaximumLength(160);
		RuleFor(request => request.Pronouns).MaximumLength(40);
		RuleFor(request => request.ContactEmail)
			.MaximumLength(200)
			.EmailAddress()
			.When(request => !string.IsNullOrWhiteSpace(request.ContactEmail));
		RuleFor(request => request.ContactPhone).MaximumLength(40);
		RuleFor(request => request.ContactAddress).MaximumLength(300);

		RuleFor(request => request.CoverPhotoUrl)
			.MaximumLength(1000)
			.Must(ProfileValidation.BeAValidHttpUrl)
			.WithMessage("Cover photo URL must be a valid http(s) URL.");

		RuleFor(request => request.WebsiteUrl)
			.MaximumLength(500)
			.Must(ProfileValidation.BeAValidHttpUrl)
			.WithMessage("Website must be a valid http(s) URL.");

		RuleFor(request => request.LinkedInUrl)
			.MaximumLength(500)
			.Must(ProfileValidation.BeAValidHttpUrl)
			.WithMessage("LinkedIn URL must be a valid http(s) URL.");

		RuleFor(request => request.TwitterUrl)
			.MaximumLength(500)
			.Must(ProfileValidation.BeAValidHttpUrl)
			.WithMessage("X/Twitter URL must be a valid http(s) URL.");

		RuleFor(request => request.InstagramUrl)
			.MaximumLength(500)
			.Must(ProfileValidation.BeAValidHttpUrl)
			.WithMessage("Instagram URL must be a valid http(s) URL.");

		RuleFor(request => request.FacebookUrl)
			.MaximumLength(500)
			.Must(ProfileValidation.BeAValidHttpUrl)
			.WithMessage("Facebook URL must be a valid http(s) URL.");

		RuleFor(request => request.YouTubeUrl)
			.MaximumLength(500)
			.Must(ProfileValidation.BeAValidHttpUrl)
			.WithMessage("YouTube URL must be a valid http(s) URL.");

		RuleFor(request => request.Languages)
			.Must(languages => languages is null || languages.Count <= 12)
			.WithMessage("Add at most 12 languages.");

		RuleForEach(request => request.Languages)
			.MaximumLength(40)
			.WithMessage("Each language must be 40 characters or fewer.");

		RuleFor(request => request.Interests)
			.Must(interests => interests is null || interests.Count <= 20)
			.WithMessage("Add at most 20 interests.");

		RuleForEach(request => request.Interests)
			.MaximumLength(60)
			.WithMessage("Each interest must be 60 characters or fewer.");

		When(request => request.Doctor is not null, () =>
		{
			RuleFor(request => request.Doctor!.ProfessionalTitle).MaximumLength(120);
			RuleFor(request => request.Doctor!.ClinicName).MaximumLength(200);
			RuleFor(request => request.Doctor!.Location).MaximumLength(200);
			RuleFor(request => request.Doctor!.Qualifications).MaximumLength(2000);
			RuleFor(request => request.Doctor!.SessionFees)
				.GreaterThanOrEqualTo(0)
				.WithMessage("Session fees cannot be negative.");
			RuleFor(request => request.Doctor!.YearsOfExperience)
				.InclusiveBetween(0, 70)
				.WithMessage("Years of experience must be between 0 and 70.");
			RuleFor(request => request.Doctor!.Specialization)
				.IsInEnum()
				.WithMessage("Specialization must be a valid value.");
		});
	}
}

public class UpdateProfilePhotoRequestValidator : AbstractValidator<UpdateProfilePhotoRequest>
{
	public UpdateProfilePhotoRequestValidator()
	{
		RuleFor(request => request.Photo).SetValidator(new PhotoValidator());
	}
}

public class CertificateRequestValidator : AbstractValidator<CertificateRequest>
{
	public CertificateRequestValidator()
	{
		RuleFor(request => request.Title)
			.NotEmpty()
			.WithMessage("Certificate title is required.")
			.MaximumLength(160);

		RuleFor(request => request.IssuingOrganization)
			.NotEmpty()
			.WithMessage("Issuing organization is required.")
			.MaximumLength(160);

		RuleFor(request => request.IssueDate)
			.NotEmpty()
			.WithMessage("Issue date is required.");

		RuleFor(request => request.ExpiryDate)
			.Must((request, expiryDate) => !expiryDate.HasValue || expiryDate.Value >= request.IssueDate)
			.WithMessage("Expiry date cannot be before the issue date.");

		RuleFor(request => request.CredentialId).MaximumLength(120);
		RuleFor(request => request.Description).MaximumLength(1000);

		RuleFor(request => request.CredentialUrl)
			.MaximumLength(500)
			.Must(ProfileValidation.BeAValidHttpUrl)
			.WithMessage("Credential URL must be a valid http(s) URL.");

		RuleFor(request => request.Image)
			.SetValidator(new PhotoValidator()!)
			.When(request => request.Image is not null);
	}
}

public class QualificationRequestValidator : AbstractValidator<QualificationRequest>
{
	public QualificationRequestValidator()
	{
		RuleFor(request => request.Degree)
			.NotEmpty()
			.WithMessage("Degree is required.")
			.MaximumLength(160);

		RuleFor(request => request.Institution)
			.NotEmpty()
			.WithMessage("Institution is required.")
			.MaximumLength(200);

		RuleFor(request => request.FieldOfStudy).MaximumLength(160);
		RuleFor(request => request.Description).MaximumLength(1000);

		RuleFor(request => request.StartYear)
			.InclusiveBetween(1950, 2100)
			.When(request => request.StartYear.HasValue)
			.WithMessage("Start year must be between 1950 and 2100.");

		RuleFor(request => request.EndYear)
			.InclusiveBetween(1950, 2100)
			.When(request => request.EndYear.HasValue)
			.WithMessage("End year must be between 1950 and 2100.");

		RuleFor(request => request.EndYear)
			.Must((request, endYear) => !endYear.HasValue || !request.StartYear.HasValue || endYear.Value >= request.StartYear.Value)
			.WithMessage("End year cannot be before the start year.");
	}
}
