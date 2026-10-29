using Application.Contracts;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class PatientDirectoryLookup : IPatientDirectoryLookup
{
	private readonly AppDbContext _dbContext;

	public PatientDirectoryLookup(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
		IReadOnlyCollection<string> userIds)
	{
		if (userIds.Count == 0)
		{
			return new Dictionary<string, string>();
		}

		// One projection for the whole page. Patient records live in the User
		// table (TPT); querying BaseUser would also return doctors and admins.
		var users = await _dbContext.Clients
			.AsNoTracking()
			.Where(user => userIds.Contains(user.Id))
			.Select(user => new { user.Id, user.FirstName, user.LastName, user.Email })
			.ToListAsync();

		return users.ToDictionary(
			user => user.Id,
			user => DisplayName(user.FirstName, user.LastName, user.Email));
	}

	public Task<bool> ExistsAsync(string userId) =>
		_dbContext.Clients.AnyAsync(user => user.Id == userId);

	private static string DisplayName(string? firstName, string? lastName, string? email)
	{
		var fullName = $"{firstName} {lastName}".Trim();
		if (fullName.Length > 0)
		{
			return fullName;
		}

		var localPart = email?.Split('@')[0];
		return string.IsNullOrWhiteSpace(localPart) ? "Patient" : localPart;
	}
}
