using Application.Dtos.SchedulingDtos;
using Shared;

namespace Application.Contracts;

public interface IDoctorSchedulingService
{
	/// <summary>Bookable UTC instants for a doctor on a local date.</summary>
	Task<Result<List<DateTime>>> GetAvailableSlotsAsync(string doctorId, DateOnly date);

	/// <summary>Validates a candidate booking time against the scheduling rules.</summary>
	Task<Result> ValidateBookingTimeAsync(string doctorId, DateTime startUtc, TimeSpan duration);

	Task<Result<List<RecurringBreakResponse>>> GetRecurringBreaksAsync(string doctorId);
	Task<Result<RecurringBreakResponse>> CreateRecurringBreakAsync(string doctorId, CreateRecurringBreakRequest request);
	Task<Result> DeleteRecurringBreakAsync(string doctorId, int id);

	Task<Result<List<AvailabilityOverrideResponse>>> GetOverridesAsync(string doctorId, DateOnly from, DateOnly to);
	Task<Result<AvailabilityOverrideResponse>> CreateOverrideAsync(string doctorId, CreateAvailabilityOverrideRequest request);
	Task<Result> DeleteOverrideAsync(string doctorId, int id);

	Task<Result<List<TimeBlockResponse>>> GetTimeBlocksAsync(string doctorId, DateTime fromUtc, DateTime toUtc);
	Task<Result<TimeBlockResponse>> CreateTimeBlockAsync(string doctorId, CreateTimeBlockRequest request);
	Task<Result<TimeBlockResponse>> CancelTimeBlockAsync(string doctorId, int id);

	Task<Result<SchedulingSettingsResponse>> GetSettingsAsync(string doctorId);
	Task<Result<SchedulingSettingsResponse>> UpdateSettingsAsync(string doctorId, UpdateSchedulingSettingsRequest request);
}
