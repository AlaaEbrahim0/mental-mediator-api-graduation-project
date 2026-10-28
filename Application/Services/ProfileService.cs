using Application.Contracts;
using Application.Dtos.ProfileDtos;
using Domain.Entities;
using Domain.Errors;
using Shared;

namespace Application.Services;

/// <summary>
/// Owns the rich public profile shared by every account type. Doctor-specific
/// practice fields are included when the account is a doctor, but the social
/// links, certificates, qualifications, languages, and interests are available
/// to patients as well.
/// </summary>
public class ProfileService : IProfileService
{
	private readonly IRepositoryManager _repoManager;
	private readonly IUserClaimsService _userClaimsService;
	private readonly IStorageService _storageService;

	public ProfileService(
		IRepositoryManager repoManager,
		IUserClaimsService userClaimsService,
		IStorageService storageService)
	{
		_repoManager = repoManager;
		_userClaimsService = userClaimsService;
		_storageService = storageService;
	}

	public async Task<Result<PublicProfileResponse>> GetPublicProfile(string id)
	{
		var profile = await _repoManager.Profiles.GetProfile(id, false);
		if (profile is null)
		{
			return UserErrors.NotFound(id);
		}

		var currentUserId = _userClaimsService.GetUserId();
		var currentRole = _userClaimsService.GetRole();
		var isOwner = !string.IsNullOrWhiteSpace(currentUserId) &&
			string.Equals(currentUserId, profile.Id, StringComparison.Ordinal);
		var isAdmin = string.Equals(currentRole, "Admin", StringComparison.OrdinalIgnoreCase);

		if (!profile.IsProfilePublic && !isOwner && !isAdmin)
		{
			// Do not reveal that a hidden profile exists.
			return ProfileErrors.HiddenProfile();
		}

		var response = new PublicProfileResponse();
		await FillCommonAsync(response, profile);

		return response;
	}

	public async Task<Result<MyProfileResponse>> GetMyProfile()
	{
		var currentUserId = _userClaimsService.GetUserId();
		var profile = await _repoManager.Profiles.GetProfile(currentUserId, false);

		if (profile is null)
		{
			return UserErrors.NotFound(currentUserId);
		}

		return await MapMyProfileAsync(profile);
	}

	public async Task<Result<MyProfileResponse>> UpdateMyProfile(UpdateProfileRequest request)
	{
		var currentUserId = _userClaimsService.GetUserId();
		var profile = await _repoManager.Profiles.GetProfile(currentUserId, true);

		if (profile is null)
		{
			return UserErrors.NotFound(currentUserId);
		}

		if (request.Doctor is not null && profile is not Doctor)
		{
			return ProfileErrors.DoctorDetailsNotAllowed();
		}

		profile.FirstName = request.FirstName.Trim();
		profile.LastName = request.LastName.Trim();
		profile.BirthDate = request.BirthDate;
		profile.Gender = request.Gender.Trim().ToLowerInvariant();
		profile.Biography = Clean(request.Biography);
		profile.PhoneNumber = Clean(request.PhoneNumber);
		profile.City = Clean(request.City);
		profile.ContactEmail = Clean(request.ContactEmail);
		profile.ContactPhone = Clean(request.ContactPhone);
		profile.ContactAddress = Clean(request.ContactAddress);

		profile.Headline = Clean(request.Headline);
		profile.Pronouns = Clean(request.Pronouns);
		profile.CoverPhotoUrl = Clean(request.CoverPhotoUrl);
		profile.WebsiteUrl = Clean(request.WebsiteUrl);
		profile.LinkedInUrl = Clean(request.LinkedInUrl);
		profile.TwitterUrl = Clean(request.TwitterUrl);
		profile.InstagramUrl = Clean(request.InstagramUrl);
		profile.FacebookUrl = Clean(request.FacebookUrl);
		profile.YouTubeUrl = Clean(request.YouTubeUrl);

		profile.Languages = NormalizeList(request.Languages);
		profile.Interests = NormalizeList(request.Interests);
		if (request.IsProfilePublic.HasValue)
		{
			profile.IsProfilePublic = request.IsProfilePublic.Value;
		}

		if (profile is Doctor doctor && request.Doctor is not null)
		{
			doctor.Location = Clean(request.Doctor.Location);
			doctor.ProfessionalTitle = Clean(request.Doctor.ProfessionalTitle);
			doctor.ClinicName = Clean(request.Doctor.ClinicName);
			doctor.Qualifications = Clean(request.Doctor.Qualifications);
			doctor.YearsOfExperience = request.Doctor.YearsOfExperience;
			doctor.SessionFees = request.Doctor.SessionFees;
			doctor.Specialization = request.Doctor.Specialization;
			doctor.IsAvailable = request.Doctor.IsAvailable;
		}

		await _repoManager.SaveAsync();

		return await MapMyProfileAsync(profile);
	}

	public async Task<Result<MyProfileResponse>> UpdateMyPhoto(UpdateProfilePhotoRequest request)
	{
		var currentUserId = _userClaimsService.GetUserId();
		var profile = await _repoManager.Profiles.GetProfile(currentUserId, true);

		if (profile is null)
		{
			return UserErrors.NotFound(currentUserId);
		}

		var uploadResult = await _storageService.UploadPhoto(request.Photo);
		if (uploadResult.IsFailure)
		{
			return uploadResult.Error;
		}

		profile.PhotoUrl = uploadResult.Value;
		await _repoManager.SaveAsync();

		return await MapMyProfileAsync(profile);
	}

	public async Task<Result<CertificateResponse>> AddCertificate(CertificateRequest request)
	{
		var currentUserId = _userClaimsService.GetUserId();

		string? imageUrl = null;
		if (request.Image is not null)
		{
			var uploadResult = await _storageService.UploadPhoto(request.Image);
			if (uploadResult.IsFailure)
			{
				return uploadResult.Error;
			}
			imageUrl = uploadResult.Value;
		}

		var certificate = new UserCertificate
		{
			UserId = currentUserId,
			Title = request.Title.Trim(),
			IssuingOrganization = request.IssuingOrganization.Trim(),
			IssueDate = request.IssueDate,
			ExpiryDate = request.ExpiryDate,
			CredentialId = Clean(request.CredentialId),
			CredentialUrl = Clean(request.CredentialUrl),
			Description = Clean(request.Description),
			ImageUrl = imageUrl,
		};

		_repoManager.Profiles.AddCertificate(certificate);
		await _repoManager.SaveAsync();

		return MapCertificate(certificate);
	}

	public async Task<Result<CertificateResponse>> UpdateCertificate(int certificateId, CertificateRequest request)
	{
		var currentUserId = _userClaimsService.GetUserId();
		var certificate = await _repoManager.Profiles.GetCertificate(currentUserId, certificateId, true);

		if (certificate is null)
		{
			return ProfileErrors.CertificateNotFound(certificateId);
		}

		if (request.Image is not null)
		{
			var uploadResult = await _storageService.UploadPhoto(request.Image);
			if (uploadResult.IsFailure)
			{
				return uploadResult.Error;
			}
			certificate.ImageUrl = uploadResult.Value;
		}

		certificate.Title = request.Title.Trim();
		certificate.IssuingOrganization = request.IssuingOrganization.Trim();
		certificate.IssueDate = request.IssueDate;
		certificate.ExpiryDate = request.ExpiryDate;
		certificate.CredentialId = Clean(request.CredentialId);
		certificate.CredentialUrl = Clean(request.CredentialUrl);
		certificate.Description = Clean(request.Description);

		_repoManager.Profiles.UpdateCertificate(certificate);
		await _repoManager.SaveAsync();

		return MapCertificate(certificate);
	}

	public async Task<Result> DeleteCertificate(int certificateId)
	{
		var currentUserId = _userClaimsService.GetUserId();
		var certificate = await _repoManager.Profiles.GetCertificate(currentUserId, certificateId, true);

		if (certificate is null)
		{
			return ProfileErrors.CertificateNotFound(certificateId);
		}

		_repoManager.Profiles.DeleteCertificate(certificate);
		await _repoManager.SaveAsync();

		return Result.Success();
	}

	public async Task<Result<QualificationResponse>> AddQualification(QualificationRequest request)
	{
		var currentUserId = _userClaimsService.GetUserId();

		var qualification = new UserQualification
		{
			UserId = currentUserId,
			Degree = request.Degree.Trim(),
			Institution = request.Institution.Trim(),
			FieldOfStudy = Clean(request.FieldOfStudy),
			StartYear = request.StartYear,
			EndYear = request.EndYear,
			Description = Clean(request.Description),
		};

		_repoManager.Profiles.AddQualification(qualification);
		await _repoManager.SaveAsync();

		return MapQualification(qualification);
	}

	public async Task<Result<QualificationResponse>> UpdateQualification(int qualificationId, QualificationRequest request)
	{
		var currentUserId = _userClaimsService.GetUserId();
		var qualification = await _repoManager.Profiles.GetQualification(currentUserId, qualificationId, true);

		if (qualification is null)
		{
			return ProfileErrors.QualificationNotFound(qualificationId);
		}

		qualification.Degree = request.Degree.Trim();
		qualification.Institution = request.Institution.Trim();
		qualification.FieldOfStudy = Clean(request.FieldOfStudy);
		qualification.StartYear = request.StartYear;
		qualification.EndYear = request.EndYear;
		qualification.Description = Clean(request.Description);

		_repoManager.Profiles.UpdateQualification(qualification);
		await _repoManager.SaveAsync();

		return MapQualification(qualification);
	}

	public async Task<Result> DeleteQualification(int qualificationId)
	{
		var currentUserId = _userClaimsService.GetUserId();
		var qualification = await _repoManager.Profiles.GetQualification(currentUserId, qualificationId, true);

		if (qualification is null)
		{
			return ProfileErrors.QualificationNotFound(qualificationId);
		}

		_repoManager.Profiles.DeleteQualification(qualification);
		await _repoManager.SaveAsync();

		return Result.Success();
	}

	private async Task<MyProfileResponse> MapMyProfileAsync(BaseUser profile)
	{
		var response = new MyProfileResponse
		{
			Email = profile.Email ?? string.Empty,
			BirthDate = profile.BirthDate,
			Gender = profile.Gender,
			PhoneNumber = profile.PhoneNumber,
		};

		await FillCommonAsync(response, profile);
		return response;
	}

	private async Task FillCommonAsync(PublicProfileResponse response, BaseUser profile)
	{
		response.Id = profile.Id;
		response.Role = ResolveRole(profile);
		response.FirstName = profile.FirstName;
		response.LastName = profile.LastName;
		response.FullName = profile.FullName;
		response.PhotoUrl = profile.PhotoUrl;
		response.CoverPhotoUrl = profile.CoverPhotoUrl;
		response.Headline = profile.Headline;
		response.Pronouns = profile.Pronouns;
		response.Biography = profile.Biography;
		response.City = profile.City;
		response.Languages = SplitList(profile.Languages);
		response.Interests = SplitList(profile.Interests);
		response.IsProfilePublic = profile.IsProfilePublic;
		response.SocialLinks = new SocialLinksResponse
		{
			WebsiteUrl = profile.WebsiteUrl,
			LinkedInUrl = profile.LinkedInUrl,
			TwitterUrl = profile.TwitterUrl,
			InstagramUrl = profile.InstagramUrl,
			FacebookUrl = profile.FacebookUrl,
			YouTubeUrl = profile.YouTubeUrl,
		};
		response.Contact = new ContactInfoResponse
		{
			Email = profile.ContactEmail,
			Phone = profile.ContactPhone,
			Address = profile.ContactAddress,
		};

		response.Certificates = profile.Certificates
			.OrderByDescending(certificate => certificate.IssueDate)
			.ThenByDescending(certificate => certificate.Id)
			.Select(MapCertificate)
			.ToList();

		response.Qualifications = profile.Education
			.OrderByDescending(qualification => qualification.EndYear ?? qualification.StartYear ?? 0)
			.ThenByDescending(qualification => qualification.Id)
			.Select(MapQualification)
			.ToList();

		if (profile is Doctor doctor)
		{
			response.Doctor = new DoctorProfileResponse
			{
				Specialization = doctor.Specialization,
				ProfessionalTitle = doctor.ProfessionalTitle,
				ClinicName = doctor.ClinicName,
				Location = doctor.Location,
				Qualifications = doctor.Qualifications,
				YearsOfExperience = doctor.YearsOfExperience,
				SessionFees = doctor.SessionFees,
				IsAvailable = doctor.IsAvailable,
				CompletedAppointments = await _repoManager.Appointments
					.GetDoctorCompletedAppointmentsCount(doctor.Id),
			};
		}
	}

	private static CertificateResponse MapCertificate(UserCertificate certificate) => new()
	{
		Id = certificate.Id,
		Title = certificate.Title,
		IssuingOrganization = certificate.IssuingOrganization,
		IssueDate = certificate.IssueDate,
		ExpiryDate = certificate.ExpiryDate,
		IsExpired = certificate.ExpiryDate.HasValue &&
			certificate.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow),
		CredentialId = certificate.CredentialId,
		CredentialUrl = certificate.CredentialUrl,
		Description = certificate.Description,
		ImageUrl = certificate.ImageUrl,
	};

	private static QualificationResponse MapQualification(UserQualification qualification) => new()
	{
		Id = qualification.Id,
		Degree = qualification.Degree,
		Institution = qualification.Institution,
		FieldOfStudy = qualification.FieldOfStudy,
		StartYear = qualification.StartYear,
		EndYear = qualification.EndYear,
		Description = qualification.Description,
	};

	private static string ResolveRole(BaseUser profile) => profile switch
	{
		Doctor => "Doctor",
		Admin => "Admin",
		User => "User",
		_ => "User",
	};

	private static string? Clean(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	private static List<string> SplitList(string? value) =>
		string.IsNullOrWhiteSpace(value)
			? new List<string>()
			: value
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

	private static string? NormalizeList(IEnumerable<string>? values)
	{
		if (values is null)
		{
			return null;
		}

		var normalized = values
			.Select(value => value?.Trim())
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value!)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		return normalized.Count == 0 ? null : string.Join(", ", normalized);
	}
}
