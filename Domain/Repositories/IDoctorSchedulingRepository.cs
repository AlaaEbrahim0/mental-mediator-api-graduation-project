using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// Scheduling primitives that describe when a doctor is *not* bookable:
/// recurring weekly breaks, specific date overrides and one-off time blocks.
/// All three are read by the availability calculation; none of them modify
/// appointments by themselves, so a confirmed appointment is never silently
/// invalidated by a schedule change.
/// </summary>
public interface IDoctorSchedulingRepository
{
	Task<IEnumerable<DoctorRecurringBreak>> GetActiveRecurringBreaks(string doctorId, bool trackChanges);

	Task<IEnumerable<DoctorAvailabilityOverride>> GetActiveOverrides(string doctorId, DateOnly from, DateOnly to, bool trackChanges);

	Task<IEnumerable<DoctorTimeBlock>> GetActiveTimeBlocks(string doctorId, DateTime fromUtc, DateTime toUtc, bool trackChanges);

	Task<DoctorSchedulingSettings?> GetSettings(string doctorId, bool trackChanges);

	Task<DoctorAvailabilityOverride?> GetOverride(int id, bool trackChanges);
	Task<DoctorRecurringBreak?> GetRecurringBreak(int id, bool trackChanges);
	Task<DoctorTimeBlock?> GetTimeBlock(int id, bool trackChanges);

	void CreateOverride(DoctorAvailabilityOverride availabilityOverride);
	void UpdateOverride(DoctorAvailabilityOverride availabilityOverride);
	void DeleteOverride(DoctorAvailabilityOverride availabilityOverride);

	void CreateRecurringBreak(DoctorRecurringBreak recurringBreak);
	void UpdateRecurringBreak(DoctorRecurringBreak recurringBreak);
	void DeleteRecurringBreak(DoctorRecurringBreak recurringBreak);

	void CreateTimeBlock(DoctorTimeBlock timeBlock);
	void UpdateTimeBlock(DoctorTimeBlock timeBlock);
	void DeleteTimeBlock(DoctorTimeBlock timeBlock);

	void CreateSettings(DoctorSchedulingSettings settings);
	void UpdateSettings(DoctorSchedulingSettings settings);
}
