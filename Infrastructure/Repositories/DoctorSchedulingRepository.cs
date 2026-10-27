using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DoctorSchedulingRepository : RepositoryBase<DoctorAvailabilityPeriod>, IDoctorSchedulingRepository
{
	public DoctorSchedulingRepository(AppDbContext dbContext) : base(dbContext) { }

	public async Task<IReadOnlyList<DoctorAvailabilityPeriod>> GetAvailabilityPeriods(string doctorId, bool trackChanges)
	{
		return await _dbContext.DoctorAvailabilityPeriods
			.AsTracking(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
			.Where(x => x.DoctorId == doctorId)
			.OrderBy(x => x.DayOfWeek)
			.ThenBy(x => x.StartTime)
			.ToListAsync();
	}

	public void CreateAvailabilityPeriod(DoctorAvailabilityPeriod period)
	{
		Create(period);
	}

	public void DeleteAvailabilityPeriod(DoctorAvailabilityPeriod period)
	{
		Delete(period);
	}

	public async Task<IReadOnlyList<DoctorTimeOff>> GetTimeOff(string doctorId, DateTime fromUtc, DateTime toUtc, bool trackChanges)
	{
		return await _dbContext.DoctorTimeOff
			.AsTracking(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
			.Where(x => x.DoctorId == doctorId && x.StartTimeUtc < toUtc && x.EndTimeUtc > fromUtc)
			.OrderBy(x => x.StartTimeUtc)
			.ToListAsync();
	}

	public async Task<DoctorTimeOff?> GetTimeOff(int id, bool trackChanges)
	{
		return await _dbContext.DoctorTimeOff
			.AsTracking(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
			.FirstOrDefaultAsync(x => x.Id == id);
	}

	public void CreateTimeOff(DoctorTimeOff timeOff)
	{
		_dbContext.DoctorTimeOff.Add(timeOff);
	}

	public void DeleteTimeOff(DoctorTimeOff timeOff)
	{
		_dbContext.DoctorTimeOff.Remove(timeOff);
	}

	public async Task<DoctorSchedulingSettings?> GetSettings(string doctorId, bool trackChanges)
	{
		return await _dbContext.DoctorSchedulingSettings
			.AsTracking(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
			.FirstOrDefaultAsync(x => x.DoctorId == doctorId);
	}

	public void CreateSettings(DoctorSchedulingSettings settings)
	{
		_dbContext.DoctorSchedulingSettings.Add(settings);
	}

	public void UpdateSettings(DoctorSchedulingSettings settings)
	{
		_dbContext.DoctorSchedulingSettings.Update(settings);
	}
}
