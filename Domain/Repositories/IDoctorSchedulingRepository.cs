using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// Persistence for the two scheduling inputs (weekly periods and time off)
/// and the doctor-level scheduling settings.
/// </summary>
public interface IDoctorSchedulingRepository
{
	Task<IReadOnlyList<DoctorAvailabilityPeriod>> GetAvailabilityPeriods(string doctorId, bool trackChanges);

	void CreateAvailabilityPeriod(DoctorAvailabilityPeriod period);
	void DeleteAvailabilityPeriod(DoctorAvailabilityPeriod period);

	Task<IReadOnlyList<DoctorTimeOff>> GetTimeOff(string doctorId, DateTime fromUtc, DateTime toUtc, bool trackChanges);
	Task<DoctorTimeOff?> GetTimeOff(int id, bool trackChanges);
	void CreateTimeOff(DoctorTimeOff timeOff);
	void DeleteTimeOff(DoctorTimeOff timeOff);

	Task<DoctorSchedulingSettings?> GetSettings(string doctorId, bool trackChanges);
	void CreateSettings(DoctorSchedulingSettings settings);
	void UpdateSettings(DoctorSchedulingSettings settings);
}
