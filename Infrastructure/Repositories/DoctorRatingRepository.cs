using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DoctorRatingRepository : IDoctorRatingRepository
{
	private readonly AppDbContext _dbContext;

	public DoctorRatingRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<DoctorRating?> GetForRaterAsync(string doctorId, string raterUserId, bool trackChanges)
	{
		var query = _dbContext.DoctorRatings.AsQueryable();
		if (!trackChanges)
		{
			query = query.AsNoTracking();
		}

		return await query
			.Where(r => r.DoctorId == doctorId && r.RaterUserId == raterUserId)
			.FirstOrDefaultAsync();
	}

	public async Task<DoctorRatingSummary?> GetSummaryAsync(string doctorId)
	{
		var group = await _dbContext.DoctorRatings
			.AsNoTracking()
			.Where(r => r.DoctorId == doctorId)
			.GroupBy(r => 1)
			.Select(g => new DoctorRatingSummary(
				Math.Round(g.Average(r => (double)r.Stars), 1), g.Count()))
			.FirstOrDefaultAsync();

		return group;
	}

	public async Task<IReadOnlyDictionary<string, DoctorRatingSummary>> GetSummariesAsync(IEnumerable<string> doctorIds)
	{
		var ids = doctorIds.Distinct().ToArray();
		if (ids.Length == 0)
		{
			return new Dictionary<string, DoctorRatingSummary>();
		}

		// (double) cast is load-bearing: AVG over an int column truncates in
		// SQL Server, so 4+5 would report 4.
		var rows = await _dbContext.DoctorRatings
			.AsNoTracking()
			.Where(r => ids.Contains(r.DoctorId))
			.GroupBy(r => r.DoctorId)
			.Select(g => new
			{
				DoctorId = g.Key,
				Average = Math.Round(g.Average(r => (double)r.Stars), 1),
				Count = g.Count(),
			})
			.ToListAsync();

		return rows.ToDictionary(r => r.DoctorId, r => new DoctorRatingSummary(r.Average, r.Count));
	}

	public async Task<IReadOnlyList<DoctorRating>> GetRecentAsync(string doctorId, int take)
	{
		return await _dbContext.DoctorRatings
			.AsNoTracking()
			.Where(r => r.DoctorId == doctorId)
			.OrderByDescending(r => r.UpdatedAtUtc ?? r.CreatedAtUtc)
			.Take(take)
			.ToListAsync();
	}

	public void Create(DoctorRating rating) => _dbContext.DoctorRatings.Add(rating);

	public void Delete(DoctorRating rating) => _dbContext.DoctorRatings.Remove(rating);
}
