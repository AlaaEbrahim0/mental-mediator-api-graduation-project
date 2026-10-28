using Application.Dtos.ProfileDtos;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace MentalMediator.Tests;

public class ProfileValidationTests
{
	private static UpdateProfileRequest ValidProfileRequest() => new()
	{
		FirstName = "Maya",
		LastName = "Hassan",
		BirthDate = new DateOnly(1990, 3, 12),
		Gender = "female",
		Biography = "Clinical psychologist supporting adults with anxiety.",
		City = "Cairo",
		ContactEmail = "hello@practice.example",
		ContactPhone = "+20 100 000 0000",
		ContactAddress = "12 Nile Street, Cairo",
		Headline = "Anxiety and stress specialist",
		Languages = new List<string> { "Arabic", "English" },
		Interests = new List<string> { "Anxiety", "Mindfulness" },
		IsProfilePublic = true,
		Doctor = new UpdateDoctorProfileRequest
		{
			Location = "New Cairo",
			ProfessionalTitle = "Consultant",
			ClinicName = "Nexus Clinic",
			YearsOfExperience = 8,
			SessionFees = 450,
			Specialization = DoctorSpecialization.ClinicalPsychology,
			IsAvailable = true,
		},
	};

	[Fact]
	public void Valid_profile_request_passes_validation()
	{
		var result = new UpdateProfileRequestValidator().Validate(ValidProfileRequest());

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Birth_date_is_required()
	{
		var request = ValidProfileRequest();
		request.BirthDate = default;

		var result = new UpdateProfileRequestValidator().Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error =>
			error.PropertyName == nameof(UpdateProfileRequest.BirthDate));
	}

	[Fact]
	public void Future_birth_date_is_rejected()
	{
		var request = ValidProfileRequest();
		request.BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

		var result = new UpdateProfileRequestValidator().Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error =>
			error.PropertyName == nameof(UpdateProfileRequest.BirthDate));
	}

	[Fact]
	public void Public_contact_email_must_be_a_valid_address()
	{
		var request = ValidProfileRequest();
		request.ContactEmail = "not-an-email";

		var result = new UpdateProfileRequestValidator().Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error =>
			error.PropertyName == nameof(UpdateProfileRequest.ContactEmail));
	}

	[Fact]
	public void Social_links_must_use_http_or_https()
	{
		var request = ValidProfileRequest();
		request.InstagramUrl = "javascript:alert(1)";

		var result = new UpdateProfileRequestValidator().Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error =>
			error.PropertyName == nameof(UpdateProfileRequest.InstagramUrl));
	}

	[Fact]
	public void Profile_lists_respect_their_limits()
	{
		var request = ValidProfileRequest();
		request.Languages = Enumerable.Range(1, 13)
			.Select(index => $"Language {index}")
			.ToList();

		var result = new UpdateProfileRequestValidator().Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error =>
			error.PropertyName == nameof(UpdateProfileRequest.Languages));
	}

	[Fact]
	public void Null_optional_collections_are_allowed()
	{
		var request = ValidProfileRequest();
		request.Languages = null!;
		request.Interests = null!;

		var result = new UpdateProfileRequestValidator().Validate(request);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Missing_gender_is_rejected_without_throwing()
	{
		var request = ValidProfileRequest();
		request.Gender = null!;

		var result = new UpdateProfileRequestValidator().Validate(request);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error =>
			error.PropertyName == nameof(UpdateProfileRequest.Gender));
	}

	[Fact]
	public void Certificate_expiry_cannot_precede_the_issue_date()
	{
		var result = new CertificateRequestValidator().Validate(new CertificateRequest
		{
			Title = "Clinical license",
			IssuingOrganization = "Ministry of Health",
			IssueDate = new DateOnly(2024, 5, 2),
			ExpiryDate = new DateOnly(2024, 5, 1),
		});

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error =>
			error.PropertyName == nameof(CertificateRequest.ExpiryDate));
	}

	[Fact]
	public void Qualification_end_year_cannot_precede_the_start_year()
	{
		var result = new QualificationRequestValidator().Validate(new QualificationRequest
		{
			Degree = "MSc Clinical Psychology",
			Institution = "Cairo University",
			StartYear = 2020,
			EndYear = 2019,
		});

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error =>
			error.PropertyName == nameof(QualificationRequest.EndYear));
	}
}
