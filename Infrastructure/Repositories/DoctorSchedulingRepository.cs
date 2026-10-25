using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DoctorSchedulingRepository : RepositoryBase<DoctorRecurringBreak>, IDoctorSchedulingRepository
{
	public DoctorSchedulingRepository(AppDbContext dbContext) : base(dbContext) { }

	public async Task<IEnumerable<DoctorRecurringBreak>> GetActiveRecurringBreaks(string doctorId, bool trackChanges)
	{
		return await FindByCondition(x => x.DoctorId == doctorId && x.IsActive, trackChanges)
			.OrderBy(x => x.DayOfWeek)
			.ThenBy(x => x.StartTime)
			.ToListAsync();
	}

	public async Task<IEnumerable<DoctorAvailabilityOverride>> GetActiveOverrides(string doctorId, DateOnly from, DateOnly to, bool trackChanges)
	{
		return await _dbContext.DoctorAvailabilityOverrides
			.AsTracking(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
			.Where(x => x.DoctorId == doctorId && x.IsActive && x.Date >= from && x.Date <= to)
			.OrderBy(x => x.Date)
			.ThenBy(x => x.StartTime)
			.ToListAsync();
	}

	public async Task<IEnumerable<DoctorTimeBlock>> GetActiveTimeBlocks(string doctorId, DateTime fromUtc, DateTime toUtc, bool trackChanges)
	{
		return await _dbContext.DoctorTimeBlocks
			.AsTracking(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
			.Where(x => x.DoctorId == doctorId && !x.IsCancelled && x.EndTimeUtc > fromUtc && x.StartTimeUtc < toUtc)
			.OrderBy(x => x.StartTimeUtc)
			.ToListAsync();
	}

	public async Task<DoctorSchedulingSettings?> GetSettings(string doctorId, bool trackChanges)
	{
		return await _dbContext.DoctorSchedulingSettings
			.AsTracking(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
			.FirstOrDefaultAsync(x => x.DoctorId == doctorId);
	}

	public async Task<DoctorAvailabilityOverride?> GetOverride(int id, bool trackChanges)
	{
		return await _dbContext.DoctorAvailabilityOverrides
			.AsTracking(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
			.FirstOrDefaultAsync(x => x.Id == id);
	}

	public async Task<DoctorRecurringBreak?> GetRecurringBreak(int id, bool trackChanges)
	{
		return await FindByCondition(x => x.Id == id, trackChanges)
			.FirstOrDefaultAsync();
	}

	public async Task<DoctorTimeBlock?> GetTimeBlock(int id, bool trackChanges)
	{
		return await _dbContext.DoctorTimeBlocks
			.AsTracking(trackChanges ? QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking)
			.FirstOrDefaultAsync(x => x.Id == id);
	}

	public void CreateOverride(DoctorAvailabilityOverride availabilityOverride)
	{
		_dbContext.DoctorAvailabilityOverrides.Add(availabilityOverride);
	}

	public void UpdateOverride(DoctorAvailabilityOverride availabilityOverride)
	{
		_dbContext.DoctorAvailabilityOverrides.Update(availabilityOverride);
	}

	public void DeleteOverride(DoctorAvailabilityOverride availabilityOverride)
	{
		_dbContext.DoctorAvailabilityOverrides.Remove(availabilityOverride);
	}

	public void CreateRecurringBreak(DoctorRecurringBreak recurringBreak)
	{
		Create(recurringBreak);
	}

	public void UpdateRecurringBreak(DoctorRecurringBreak recurringBreak)
	{
		Update(recurringBreak);
	}

	public void DeleteRecurringBreak(DoctorRecurringBreak recurringBreak)
	{
		Delete(recurringBreak);
	}

	public void CreateTimeBlock(DoctorTimeBlock timeBlock)
	{
		_dbContext.DoctorTimeBlocks.Add(timeBlock);
	}

	public void UpdateTimeBlock(DoctorTimeBlock timeBlock)
	{
		_dbContext.DoctorTimeBlocks.Update(timeBlock);
	}

	public void DeleteTimeBlock(DoctorTimeBlock timeBlock)
	{
		_dbContext.DoctorTimeBlocks.Remove(timeBlock);
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
