using Application.Contracts;
using Application.Dtos.SchedulingDtos;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Shared;

namespace Application.Services;

/// <summary>
/// Manages the scheduling primitives (recurring breaks, date overrides, time
/// blocks, scheduling settings) and computes bookable slots from them.
///
/// Availability is derived from four inputs, in this order:
///   1. the doctor's working hours for the weekday (<c>DoctorScheduleWeekDay</c>),
///      unless an availability override replaces or cancels them for the date;
///   2. the doctor's recurring weekly breaks;
///   3. one-off UTC time blocks;
///   4. appointments that already reserve the doctor's time.
///
/// A slot is only offered when it fits inside working hours, clears every
/// buffer, and does not overlap anything above. Because blocks are stored in
/// UTC while working hours are wall-clock, the calculation happens in the
/// doctor's time zone and converts back to UTC for the booked appointment.
/// </summary>
public class DoctorSchedulingService : IDoctorSchedulingService
{
	private readonly IRepositoryManager _repoManager;
	private readonly IUserClaimsService _userClaimsService;
	private readonly IMapper _mapper;

	/// <summary>Working-hours fallback when a doctor has no settings row yet.</summary>
	private static readonly DoctorSchedulingSettings DefaultSettings = new();

	private static DoctorSchedulingSettings WithDoctor(DoctorSchedulingSettings settings, string doctorId)
	{
		return new DoctorSchedulingSettings
		{
			DoctorId = doctorId,
			TimeZoneId = settings.TimeZoneId,
			SlotIncrementMinutes = settings.SlotIncrementMinutes,
			BufferBeforeMinutes = settings.BufferBeforeMinutes,
			BufferAfterMinutes = settings.BufferAfterMinutes,
			MinimumNoticeMinutes = settings.MinimumNoticeMinutes,
			MaxBookingDaysAhead = settings.MaxBookingDaysAhead,
		};
	}

	public DoctorSchedulingService(
		IRepositoryManager repoManager,
		IUserClaimsService userClaimsService,
		IMapper mapper)
	{
		_repoManager = repoManager;
		_userClaimsService = userClaimsService;
		_mapper = mapper;
	}

	/// <summary>
	/// Returns the bookable UTC instants for a doctor on a given local date.
	/// The result is ordered ascending; an empty list means nothing is bookable.
	/// </summary>
	public async Task<Result<List<DateTime>>> GetAvailableSlotsAsync(string doctorId, DateOnly date)
	{
		if (!DateOnly.TryParse(date.ToString("yyyy-MM-dd"), out var parsed))
		{
			return Error.Validation("Scheduling.InvalidDate", "Date must be a valid calendar date.");
		}

		var settings = await GetSettingsForCalculationAsync(doctorId);
		if (settings is null)
		{
			return Error.NotFound("Scheduling.SettingsNotFound", "Doctor scheduling settings could not be resolved.");
		}

		TimeZoneInfo zone;
		try
		{
			zone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
		}
		catch (TimeZoneNotFoundException)
		{
			return Error.Failure("Scheduling.InvalidTimeZone", $"Time zone '{settings.TimeZoneId}' is not available on this host.");
		}
		catch (InvalidTimeZoneException)
		{
			return Error.Failure("Scheduling.InvalidTimeZone", $"Time zone '{settings.TimeZoneId}' is not usable on this host.");
		}

		var workingHours = await ResolveWorkingHoursAsync(doctorId, parsed);
		if (workingHours is null)
		{
			// No working hours means nothing is bookable on this date; the old
			// behaviour of a 404 here hid that from the client.
			return new List<DateTime>();
		}

		var (dayStartUtc, dayEndUtc) = ToUtcRange(parsed, zone);

		var breaks = await _repoManager.DoctorScheduling.GetActiveRecurringBreaks(doctorId, false);
		var overrides = await _repoManager.DoctorScheduling.GetActiveOverrides(doctorId, parsed, parsed, false);
		var blocks = await _repoManager.DoctorScheduling.GetActiveTimeBlocks(doctorId, dayStartUtc, dayEndUtc, false);
		var appointments = await _repoManager.Appointments.GetActiveAppointmentsByDoctorIdInRange(doctorId, dayStartUtc, dayEndUtc, false);

		var busyUtc = new List<(DateTime StartUtc, DateTime EndUtc)>();
		// Busy windows in the doctor's wall-clock, used to test wall-clock
		// candidates against breaks and overrides without any conversion.
		var busyLocal = new List<(TimeSpan Start, TimeSpan End)>();

		foreach (var block in blocks)
		{
			busyUtc.Add((block.StartTimeUtc, block.EndTimeUtc));
		}

		foreach (var appointment in appointments)
		{
			var end = appointment.StartTime.Add(appointment.Duration);
			if (end <= dayStartUtc || appointment.StartTime >= dayEndUtc)
			{
				continue;
			}

			busyUtc.Add((appointment.StartTime, end));
		}

		// Recurring breaks apply to this weekday and are already wall-clock.
		foreach (var recurringBreak in breaks.Where(x => x.DayOfWeek == parsed.DayOfWeek))
		{
			busyLocal.Add((recurringBreak.StartTime, recurringBreak.EndTime));
		}

		// Date overrides that are not FullDayLeave already shaped workingHours,
		// so only the interval-based ones add busy time.
		foreach (var availabilityOverride in overrides)
		{
			if (availabilityOverride.Type is not DoctorAvailabilityOverrideType.UnavailableInterval
				or DoctorAvailabilityOverrideType.ReplaceWorkingHours)
			{
				continue;
			}

			if (availabilityOverride.StartTime is null || availabilityOverride.EndTime is null)
			{
				continue;
			}

			busyLocal.Add((availabilityOverride.StartTime.Value, availabilityOverride.EndTime.Value));
		}

		busyUtc = MergeUtc(busyUtc);

		var slots = new List<DateTime>();
		var increment = TimeSpan.FromMinutes(settings.SlotIncrementMinutes);
		var duration = workingHours.Value.Duration;
		var bufferBefore = TimeSpan.FromMinutes(settings.BufferBeforeMinutes);
		var bufferAfter = TimeSpan.FromMinutes(settings.BufferAfterMinutes);

		// Walk in the doctor's wall-clock so working hours, breaks and overrides
		// (all TimeSpan) compare directly. UTC busy windows are converted back
		// to wall-clock for the same comparison, then the surviving candidates
		// are converted to UTC once for the response.
		var busyLocalFromUtc = busyUtc
			.Select(window => (Start: TimeZoneInfo.ConvertTimeFromUtc(window.StartUtc, zone).TimeOfDay,
				End: TimeZoneInfo.ConvertTimeFromUtc(window.EndUtc, zone).TimeOfDay))
			.ToList();
		busyLocal.AddRange(busyLocalFromUtc);

		var earliestAllowed = dayStartUtc.AddMinutes(settings.MinimumNoticeMinutes);

		var candidate = workingHours.Value.Start;
		while (candidate + duration <= workingHours.Value.End)
		{
			var windowStart = candidate - bufferBefore;
			var windowEnd = candidate + duration + bufferAfter;

			var overlapsBusy = busyLocal.Any(busy => windowStart < busy.End && windowEnd > busy.Start);
			if (overlapsBusy)
			{
				candidate += increment;
				continue;
			}

			var candidateUtc = Combine(parsed, candidate, zone);
			if (candidateUtc < earliestAllowed)
			{
				candidate += increment;
				continue;
			}

			slots.Add(candidateUtc);
			candidate += increment;
		}

		return slots.OrderBy(slot => slot).ToList();
	}

	/// <summary>
	/// Rejects a candidate appointment time against the same rules the slot
	/// calculator uses. Used by booking so a client cannot request a slot the
	/// calculator would never have offered.
	/// </summary>
	public async Task<Result> ValidateBookingTimeAsync(string doctorId, DateTime startUtc, TimeSpan duration)
	{
		var settings = await GetSettingsForCalculationAsync(doctorId);
		if (settings is null)
		{
			return Error.Conflict("Scheduling.SettingsNotFound", "Doctor scheduling settings could not be resolved.");
		}

		var endUtc = startUtc + duration;
		if (duration <= TimeSpan.Zero)
		{
			return Error.Validation("Appointments.InvalidDuration", "Duration must be greater than zero.");
		}

		TimeZoneInfo zone;
		try
		{
			zone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
		}
		catch (Exception)
		{
			return Error.Failure("Scheduling.InvalidTimeZone", $"Time zone '{settings.TimeZoneId}' is not available on this host.");
		}

		var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(startUtc, zone));

		if (localDate > DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone))
			.AddDays(settings.MaxBookingDaysAhead))
		{
			return Error.Validation("Scheduling.TooFarAhead", $"Appointments can only be booked up to {settings.MaxBookingDaysAhead} days ahead.");
		}

		var workingHours = await ResolveWorkingHoursAsync(doctorId, localDate);
		if (workingHours is null)
		{
			return Error.Conflict("Scheduling.NotWorking", "The doctor does not work on the requested date.");
		}

		var (dayStartUtc, dayEndUtc) = ToUtcRange(localDate, zone);
		var localStart = TimeZoneInfo.ConvertTimeFromUtc(startUtc, zone);
		var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endUtc, zone);

		if (localStart.TimeOfDay < workingHours.Value.Start || localEnd.TimeOfDay > workingHours.Value.End)
		{
			return Error.Conflict("Scheduling.OutsideWorkingHours", "The requested time is outside the doctor's working hours.");
		}

		var breaks = await _repoManager.DoctorScheduling.GetActiveRecurringBreaks(doctorId, false);
		foreach (var recurringBreak in breaks.Where(x => x.DayOfWeek == localDate.DayOfWeek))
		{
			if (Overlaps(localStart.TimeOfDay, localEnd.TimeOfDay, recurringBreak.StartTime, recurringBreak.EndTime))
			{
				return Error.Conflict("Scheduling.BreakConflict", "The requested time overlaps a recurring break.");
			}
		}

		var overrides = await _repoManager.DoctorScheduling.GetActiveOverrides(doctorId, localDate, localDate, false);
		foreach (var availabilityOverride in overrides)
		{
			if (availabilityOverride.Type is not DoctorAvailabilityOverrideType.UnavailableInterval
				or DoctorAvailabilityOverrideType.ReplaceWorkingHours)
			{
				continue;
			}

			if (availabilityOverride.StartTime is null || availabilityOverride.EndTime is null)
			{
	continue;
			}

			if (Overlaps(localStart.TimeOfDay, localEnd.TimeOfDay, availabilityOverride.StartTime.Value, availabilityOverride.EndTime.Value))
			{
				return Error.Conflict("Scheduling.OverrideConflict", "The requested time is marked unavailable for that date.");
			}
		}

		var blocks = await _repoManager.DoctorScheduling.GetActiveTimeBlocks(doctorId, dayStartUtc, dayEndUtc, false);
		foreach (var block in blocks)
		{
			if (startUtc < block.EndTimeUtc && endUtc > block.StartTimeUtc)
			{
				return Error.Conflict("Scheduling.BlockConflict", "The requested time overlaps an existing time block.");
			}
		}

		var appointments = await _repoManager.Appointments.GetActiveAppointmentsByDoctorIdInRange(doctorId, startUtc, endUtc, false);
		if (appointments.Any())
		{
			return Error.Conflict("Scheduling.AppointmentConflict", "The doctor already has an appointment at the requested time.");
		}

		return Result.Success();
	}

	public async Task<Result<List<RecurringBreakResponse>>> GetRecurringBreaksAsync(string doctorId)
	{
		var breaks = await _repoManager.DoctorScheduling.GetActiveRecurringBreaks(doctorId, false);
		return _mapper.Map<List<RecurringBreakResponse>>(breaks);
	}

	public async Task<Result<RecurringBreakResponse>> CreateRecurringBreakAsync(string doctorId, CreateRecurringBreakRequest request)
	{
		if (await OwnsDoctorAsync(doctorId) == false)
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own scheduling.");
		}

		var conflict = await _repoManager.DoctorScheduling.GetActiveRecurringBreaks(doctorId, false);
		var overlaps = conflict.Any(existing =>
			existing.DayOfWeek == request.DayOfWeek &&
			Overlaps(existing.StartTime, existing.EndTime, request.StartTime, request.EndTime));

		if (overlaps)
		{
			return Error.Conflict("Scheduling.BreakOverlap", "A recurring break already covers that time on that weekday.");
		}

		var recurringBreak = new DoctorRecurringBreak
		{
			DoctorId = doctorId,
			DayOfWeek = request.DayOfWeek,
			StartTime = request.StartTime,
			EndTime = request.EndTime,
			Name = request.Name,
			IsActive = true,
		};

		_repoManager.DoctorScheduling.CreateRecurringBreak(recurringBreak);
		await _repoManager.SaveAsync();

		return _mapper.Map<RecurringBreakResponse>(recurringBreak);
	}

	public async Task<Result> DeleteRecurringBreakAsync(string doctorId, int id)
	{
		if (await OwnsDoctorAsync(doctorId) == false)
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own scheduling.");
		}

		var recurringBreak = await _repoManager.DoctorScheduling.GetRecurringBreak(id, true);
		if (recurringBreak is null || recurringBreak.DoctorId != doctorId)
		{
			return Error.NotFound("Scheduling.BreakNotFound", "Recurring break was not found.");
		}

		_repoManager.DoctorScheduling.DeleteRecurringBreak(recurringBreak);
		await _repoManager.SaveAsync();

		return Result.Success();
	}

	public async Task<Result<List<AvailabilityOverrideResponse>>> GetOverridesAsync(string doctorId, DateOnly from, DateOnly to)
	{
		var overrides = await _repoManager.DoctorScheduling.GetActiveOverrides(doctorId, from, to, false);
		return _mapper.Map<List<AvailabilityOverrideResponse>>(overrides);
	}

	public async Task<Result<AvailabilityOverrideResponse>> CreateOverrideAsync(string doctorId, CreateAvailabilityOverrideRequest request)
	{
		if (await OwnsDoctorAsync(doctorId) == false)
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own scheduling.");
		}

		var availabilityOverride = new DoctorAvailabilityOverride
		{
			DoctorId = doctorId,
			Date = request.Date,
			Type = request.Type,
			StartTime = request.StartTime,
			EndTime = request.EndTime,
			PrivateReason = request.PrivateReason,
			IsActive = true,
		};

		_repoManager.DoctorScheduling.CreateOverride(availabilityOverride);
		await _repoManager.SaveAsync();

		return _mapper.Map<AvailabilityOverrideResponse>(availabilityOverride);
	}

	public async Task<Result> DeleteOverrideAsync(string doctorId, int id)
	{
		if (await OwnsDoctorAsync(doctorId) == false)
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own scheduling.");
		}

		var availabilityOverride = await _repoManager.DoctorScheduling.GetOverride(id, true);
		if (availabilityOverride is null || availabilityOverride.DoctorId != doctorId)
		{
			return Error.NotFound("Scheduling.OverrideNotFound", "Availability override was not found.");
		}

		_repoManager.DoctorScheduling.DeleteOverride(availabilityOverride);
		await _repoManager.SaveAsync();

		return Result.Success();
	}

	public async Task<Result<List<TimeBlockResponse>>> GetTimeBlocksAsync(string doctorId, DateTime fromUtc, DateTime toUtc)
	{
		var blocks = await _repoManager.DoctorScheduling.GetActiveTimeBlocks(doctorId, fromUtc, toUtc, false);
		return _mapper.Map<List<TimeBlockResponse>>(blocks);
	}

	public async Task<Result<TimeBlockResponse>> CreateTimeBlockAsync(string doctorId, CreateTimeBlockRequest request)
	{
		if (await OwnsDoctorAsync(doctorId) == false)
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own scheduling.");
		}

		var now = DateTime.UtcNow;
		var timeBlock = new DoctorTimeBlock
		{
			DoctorId = doctorId,
			StartTimeUtc = request.StartTimeUtc,
			EndTimeUtc = request.EndTimeUtc,
			Type = request.Type,
			PrivateReason = request.PrivateReason,
			CreatedAtUtc = now,
			CreatedByUserId = _userClaimsService.GetUserId(),
			IsCancelled = false,
		};

		_repoManager.DoctorScheduling.CreateTimeBlock(timeBlock);
		await _repoManager.SaveAsync();

		return _mapper.Map<TimeBlockResponse>(timeBlock);
	}

	/// <summary>
	/// Cancels rather than deletes a time block so the audit trail of what the
	/// doctor blocked out and why survives.
	/// </summary>
	public async Task<Result<TimeBlockResponse>> CancelTimeBlockAsync(string doctorId, int id)
	{
		if (await OwnsDoctorAsync(doctorId) == false)
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own scheduling.");
		}

		var timeBlock = await _repoManager.DoctorScheduling.GetTimeBlock(id, true);
		if (timeBlock is null || timeBlock.DoctorId != doctorId)
		{
			return Error.NotFound("Scheduling.BlockNotFound", "Time block was not found.");
		}

		if (timeBlock.IsCancelled)
		{
			return Error.Conflict("Scheduling.AlreadyCancelled", "Time block is already cancelled.");
		}

		timeBlock.IsCancelled = true;
		timeBlock.CancelledAtUtc = DateTime.UtcNow;
		timeBlock.UpdatedAtUtc = DateTime.UtcNow;

		_repoManager.DoctorScheduling.UpdateTimeBlock(timeBlock);
		await _repoManager.SaveAsync();

		return _mapper.Map<TimeBlockResponse>(timeBlock);
	}

	public async Task<Result<SchedulingSettingsResponse>> GetSettingsAsync(string doctorId)
	{
		var settings = await _repoManager.DoctorScheduling.GetSettings(doctorId, false)
			?? WithDoctor(DefaultSettings, doctorId);

		return _mapper.Map<SchedulingSettingsResponse>(settings);
	}

	public async Task<Result<SchedulingSettingsResponse>> UpdateSettingsAsync(string doctorId, UpdateSchedulingSettingsRequest request)
	{
		if (await OwnsDoctorAsync(doctorId) == false)
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own scheduling.");
		}

		var settings = await _repoManager.DoctorScheduling.GetSettings(doctorId, true);
		if (settings is null)
		{
			settings = new DoctorSchedulingSettings { DoctorId = doctorId };
			_repoManager.DoctorScheduling.CreateSettings(settings);
		}
		else
		{
			_repoManager.DoctorScheduling.UpdateSettings(settings);
		}

		settings.TimeZoneId = request.TimeZoneId;
		settings.SlotIncrementMinutes = request.SlotIncrementMinutes;
		settings.BufferBeforeMinutes = request.BufferBeforeMinutes;
		settings.BufferAfterMinutes = request.BufferAfterMinutes;
		settings.MinimumNoticeMinutes = request.MinimumNoticeMinutes;
		settings.MaxBookingDaysAhead = request.MaxBookingDaysAhead;

		await _repoManager.SaveAsync();

		return _mapper.Map<SchedulingSettingsResponse>(settings);
	}

	private async Task<bool> OwnsDoctorAsync(string doctorId)
	{
		return string.Equals(_userClaimsService.GetUserId(), doctorId, StringComparison.Ordinal);
	}

	private async Task<DoctorSchedulingSettings?> GetSettingsForCalculationAsync(string doctorId)
	{
		var settings = await _repoManager.DoctorScheduling.GetSettings(doctorId, false);
		return settings ?? WithDoctor(DefaultSettings, doctorId);
	}

	/// <summary>
	/// Working hours for a date, honouring FullDayLeave and ReplaceWorkingHours
	/// overrides. Returns null when the doctor does not work that day.
	/// </summary>
	private async Task<WorkingHours?> ResolveWorkingHoursAsync(string doctorId, DateOnly date)
	{
		var overrides = await _repoManager.DoctorScheduling.GetActiveOverrides(doctorId, date, date, false);
		var fullDayLeave = overrides.FirstOrDefault(x => x.Type == DoctorAvailabilityOverrideType.FullDayLeave);
		if (fullDayLeave is not null)
		{
			return null;
		}

		var weekday = await _repoManager.DoctorSchedule.GetScheduleWeekDay(doctorId, date.DayOfWeek, false);
		if (weekday is null)
		{
			return null;
		}

		var replaced = overrides.FirstOrDefault(x => x.Type == DoctorAvailabilityOverrideType.ReplaceWorkingHours);
		if (replaced?.StartTime is not null && replaced.EndTime is not null)
		{
			return new WorkingHours(replaced.StartTime.Value, replaced.EndTime.Value);
		}

		var hours = new WorkingHours(weekday.StartTime, weekday.EndTime);

		var additional = overrides
			.Where(x => x.Type == DoctorAvailabilityOverrideType.AdditionalWorkingSession)
			.Where(x => x.StartTime is not null && x.EndTime is not null)
			.ToList();

		if (additional.Count == 0)
		{
			return hours;
		}

		// Extra sessions widen the bookable window to the earliest start and the
		// latest end across the base hours and every additional session.
		var earliest = new[] { hours.Start }.Concat(additional.Select(x => x.StartTime!.Value)).Min();
		var latest = new[] { hours.End }.Concat(additional.Select(x => x.EndTime!.Value)).Max();

		return new WorkingHours(earliest, latest);
	}

	private static (DateTime StartUtc, DateTime EndUtc) ToUtcRange(DateOnly date, TimeZoneInfo zone)
	{
		var startLocal = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
		var endLocal = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Unspecified);

		return (TimeZoneInfo.ConvertTimeToUtc(startLocal, zone), TimeZoneInfo.ConvertTimeToUtc(endLocal, zone));
	}

	private static DateTime Combine(DateOnly date, TimeSpan time, TimeZoneInfo zone)
	{
		var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromTimeSpan(time)), DateTimeKind.Unspecified);
		return TimeZoneInfo.ConvertTimeToUtc(local, zone);
	}

	private static bool Overlaps(TimeSpan start, TimeSpan end, TimeSpan otherStart, TimeSpan otherEnd)
	{
		return start < otherEnd && end > otherStart;
	}

	/// <summary>Merges overlapping UTC windows so a slot only has to clear one interval.</summary>
	private static List<(DateTime StartUtc, DateTime EndUtc)> MergeUtc(List<(DateTime StartUtc, DateTime EndUtc)> windows)
	{
		if (windows.Count <= 1)
		{
			return windows;
		}

		var ordered = windows.OrderBy(window => window.StartUtc).ToList();
		var merged = new List<(DateTime StartUtc, DateTime EndUtc)> { ordered[0] };

		foreach (var window in ordered.Skip(1))
		{
			var last = merged[^1];
			if (window.StartUtc <= last.EndUtc)
			{
				merged[^1] = (last.StartUtc, window.EndUtc > last.EndUtc ? window.EndUtc : last.EndUtc);
			}
			else
			{
				merged.Add(window);
			}
		}

		return merged;
	}

	private readonly record struct WorkingHours(TimeSpan Start, TimeSpan End)
	{
		public TimeSpan Duration => End - Start;
	}
}
