using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// Data access for the rich profile sections that are shared by every account
/// type (users, doctors, and admins).
/// </summary>
public interface IProfileRepository
{
	Task<BaseUser?> GetProfile(string userId, bool trackChanges);

	Task<UserCertificate?> GetCertificate(string userId, int certificateId, bool trackChanges);

	Task<UserQualification?> GetQualification(string userId, int qualificationId, bool trackChanges);

	void AddCertificate(UserCertificate certificate);

	void UpdateCertificate(UserCertificate certificate);

	void DeleteCertificate(UserCertificate certificate);

	void AddQualification(UserQualification qualification);

	void UpdateQualification(UserQualification qualification);

	void DeleteQualification(UserQualification qualification);
}
