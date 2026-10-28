using Application.Dtos.ProfileDtos;
using Shared;

namespace Application.Contracts;

public interface IProfileService
{
	Task<Result<PublicProfileResponse>> GetPublicProfile(string id);
	Task<Result<MyProfileResponse>> GetMyProfile();
	Task<Result<MyProfileResponse>> UpdateMyProfile(UpdateProfileRequest request);
	Task<Result<MyProfileResponse>> UpdateMyPhoto(UpdateProfilePhotoRequest request);

	Task<Result<CertificateResponse>> AddCertificate(CertificateRequest request);
	Task<Result<CertificateResponse>> UpdateCertificate(int certificateId, CertificateRequest request);
	Task<Result> DeleteCertificate(int certificateId);

	Task<Result<QualificationResponse>> AddQualification(QualificationRequest request);
	Task<Result<QualificationResponse>> UpdateQualification(int qualificationId, QualificationRequest request);
	Task<Result> DeleteQualification(int qualificationId);
}
