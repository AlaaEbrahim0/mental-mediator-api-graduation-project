using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ProfileRepository : IProfileRepository
{
	private readonly AppDbContext _dbContext;

	public ProfileRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<BaseUser?> GetProfile(string userId, bool trackChanges)
	{
		// BaseUser is mapped with table-per-type inheritance, so this query
		// returns the concrete User, Doctor, or Admin instance and its specialty
		// columns.
		var query = _dbContext.Set<BaseUser>()
			.Include(user => user.Certificates)
			.Include(user => user.Education)
			.AsQueryable();

		if (!trackChanges)
		{
			query = query.AsNoTracking();
		}

		return await query.FirstOrDefaultAsync(user => user.Id == userId);
	}

	public async Task<UserCertificate?> GetCertificate(string userId, int certificateId, bool trackChanges)
	{
		var query = _dbContext.Set<UserCertificate>()
			.Where(certificate => certificate.UserId == userId && certificate.Id == certificateId);

		if (!trackChanges)
		{
			query = query.AsNoTracking();
		}

		return await query.FirstOrDefaultAsync();
	}

	public async Task<UserQualification?> GetQualification(string userId, int qualificationId, bool trackChanges)
	{
		var query = _dbContext.Set<UserQualification>()
			.Where(qualification => qualification.UserId == userId && qualification.Id == qualificationId);

		if (!trackChanges)
		{
			query = query.AsNoTracking();
		}

		return await query.FirstOrDefaultAsync();
	}

	public void AddCertificate(UserCertificate certificate) =>
		_dbContext.Set<UserCertificate>().Add(certificate);

	public void UpdateCertificate(UserCertificate certificate) =>
		_dbContext.Set<UserCertificate>().Update(certificate);

	public void DeleteCertificate(UserCertificate certificate) =>
		_dbContext.Set<UserCertificate>().Remove(certificate);

	public void AddQualification(UserQualification qualification) =>
		_dbContext.Set<UserQualification>().Add(qualification);

	public void UpdateQualification(UserQualification qualification) =>
		_dbContext.Set<UserQualification>().Update(qualification);

	public void DeleteQualification(UserQualification qualification) =>
		_dbContext.Set<UserQualification>().Remove(qualification);
}
