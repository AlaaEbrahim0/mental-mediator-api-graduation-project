using Application.Dtos.SchedulingDtos;
using Shared;

namespace Application.Contracts;

/// <summary>
/// The single scheduling path for a doctor: weekly availability periods,
/// time off, and the consultation duration/time zone that give them meaning.
/// Slot discovery and booking validation both go through this service so they
/// cannot drift apart.
/// </summary>
public interface IDoctorSchedulingService
{
	/// <summary>Reads the weekly periods plus the doctor-level duration/time zone.</summary>
	Task<Result<DoctorScheduleResponse>> GetScheduleAsync(string doctorId);

	/// <summary>
	/// Replaces the weekly periods and settings. Rejects the change when it
	/// would invalidate a Pending or Confirmed appointment that has not ended.
	/// </summary>
	Task<Result<DoctorScheduleResponse>> UpdateScheduleAsync(string doctorId, UpdateDoctorScheduleRequest request);

	/// <summary>Time off in the doctor's local time, optionally bounded by local dates.</summary>
	Task<Result<List<TimeOffResponse>>> GetTimeOffAsync(string doctorId, DateOnly? from, DateOnly? to);

	/// <summary>
	/// Creates time off. Rejects overlaps with existing time off and with
	/// active appointments instead of silently invalidating them.
	/// </summary>
	Task<Result<TimeOffResponse>> CreateTimeOffAsync(string doctorId, CreateTimeOffRequest request);

	Task<Result> DeleteTimeOffAsync(string doctorId, int id);

	/// <summary>
	/// Dynamically calculated bookable slots for a local date range, excluding
	/// time off and Pending/Confirmed appointments.
	/// </summary>
	Task<Result<DoctorAvailabilityResponse>> GetAvailabilityAsync(string doctorId, DateOnly from, DateOnly to);

	/// <summary>
	/// Revalidates a candidate instant against the current schedule and returns
	/// the doctor's authoritative consultation duration. A client-supplied
	/// duration is accepted only when it matches the configured duration.
	/// </summary>
	Task<Result<TimeSpan>> ValidateBookingTimeAsync(string doctorId, DateTime startUtc, TimeSpan? requestedDuration);
}
