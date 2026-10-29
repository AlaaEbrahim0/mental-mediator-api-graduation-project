using Application.Contracts;
using Application.Dtos.SchedulingDtos;
using Domain.Entities;
using Shared;

namespace Application.Services;

/// <summary>
/// The single scheduling implementation for doctors.
///
/// Availability is derived, never stored as bookable slots:
///   1. weekly <see cref="DoctorAvailabilityPeriod"/> rows, interpreted in the
///      doctor's time zone;
///   2. minus <see cref="DoctorTimeOff"/> ranges;
///   3. minus Pending/Confirmed appointments.
///
/// Booking validation and slot discovery share the same helpers, so a client
/// cannot book a time the slot calculator would never have offered. Schedule
/// and time-off mutations take the per-doctor booking lock and reject changes
/// that would invalidate an active appointment.
/// </summary>
public class DoctorSchedulingService : IDoctorSchedulingService
{
	private const int MaxAvailabilityRangeDays = 62;
	private const string DefaultTimeZoneId = "UTC";

	private readonly IRepositoryManager _repoManager;
	private readonly IUserClaimsService _userClaimsService;

	public DoctorSchedulingService(
		IRepositoryManager repoManager,
		IUserClaimsService userClaimsService)
	{
		_repoManager = repoManager;
		_userClaimsService = userClaimsService;
	}

	public async Task<Result<DoctorScheduleResponse>> GetScheduleAsync(string doctorId)
	{
		if (!await DoctorExistsAsync(doctorId))
		{
			return DoctorNotFound(doctorId);
		}

		var settings = await _repoManager.DoctorScheduling.GetSettings(doctorId, false)
			?? DefaultSettings(doctorId);
		var periods = await _repoManager.DoctorScheduling.GetAvailabilityPeriods(doctorId, false);

		return BuildScheduleResponse(doctorId, settings, periods);
	}

	public async Task<Result<DoctorScheduleResponse>> UpdateScheduleAsync(
		string doctorId,
		UpdateDoctorScheduleRequest request)
	{
		if (!OwnsDoctor(doctorId))
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own schedule.");
		}

		if (!await DoctorExistsAsync(doctorId))
		{
			return DoctorNotFound(doctorId);
		}

		var periodError = ValidatePeriods(request.Periods);
		if (periodError is not null)
		{
			return periodError;
		}

		if (request.ConsultationDurationMinutes is not (30 or 45 or 60))
		{
			return Error.Validation(
				"Scheduling.InvalidDuration",
				"Consultation duration must be 30, 45, or 60 minutes.");
		}

		var zone = ResolveTimeZone(request.TimeZoneId, out var timeZoneError);
		if (zone is null)
		{
			return timeZoneError!;
		}

		// Serialize with booking and with other schedule edits for this doctor.
		await using var bookingLock = await _repoManager.Appointments.AcquireBookingLockAsync(doctorId);

		var activeAppointments = await _repoManager.Appointments
			.GetActiveAppointmentsByDoctorIdInRange(doctorId, DateTime.UtcNow.AddDays(-1), DateTime.MaxValue, false);

		var conflictingIds = activeAppointments
			.Where(appointment => appointment.StartTime + appointment.Duration > DateTime.UtcNow)
			.Where(appointment => !IsInsideAnyPeriod(
				appointment.StartTime,
				appointment.Duration,
				request.Periods,
				zone))
			.Select(appointment => appointment.Id)
			.Distinct()
			.OrderBy(id => id)
			.ToList();

		if (conflictingIds.Count > 0)
		{
			return Error.Conflict(
				"Scheduling.AppointmentConflict",
				DescribeAppointmentConflicts(
					conflictingIds,
					"this schedule change",
					"the schedule was not changed"));
		}

		var existingPeriods = await _repoManager.DoctorScheduling.GetAvailabilityPeriods(doctorId, true);
		foreach (var existingPeriod in existingPeriods)
		{
			_repoManager.DoctorScheduling.DeleteAvailabilityPeriod(existingPeriod);
		}

		foreach (var period in request.Periods.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime))
		{
			_repoManager.DoctorScheduling.CreateAvailabilityPeriod(new DoctorAvailabilityPeriod
			{
				DoctorId = doctorId,
				DayOfWeek = period.DayOfWeek,
				StartTime = period.StartTime,
				EndTime = period.EndTime,
			});
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

		settings.TimeZoneId = request.TimeZoneId.Trim();
		settings.ConsultationDurationMinutes = request.ConsultationDurationMinutes;

		await _repoManager.SaveAsync();
		await bookingLock.CommitAsync();

		var savedPeriods = await _repoManager.DoctorScheduling.GetAvailabilityPeriods(doctorId, false);
		return BuildScheduleResponse(doctorId, settings, savedPeriods);
	}

	public async Task<Result<List<TimeOffResponse>>> GetTimeOffAsync(
		string doctorId,
		DateOnly? from,
		DateOnly? to)
	{
		if (!OwnsDoctor(doctorId))
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only view your own time off.");
		}

		if (from.HasValue != to.HasValue)
		{
			return Error.Validation(
				"Scheduling.InvalidRange",
				"Provide both the start and end date, or neither.");
		}

		if (from.HasValue && to.HasValue && to.Value < from.Value)
		{
			return Error.Validation("Scheduling.InvalidRange", "The end date must not precede the start date.");
		}

		var settings = await _repoManager.DoctorScheduling.GetSettings(doctorId, false)
			?? DefaultSettings(doctorId);
		var zone = ResolveTimeZone(settings.TimeZoneId, out var timeZoneError);
		if (zone is null)
		{
			return timeZoneError!;
		}

		var fromUtc = from.HasValue
			? StartOfLocalDateUtc(from.Value, zone)
			: DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
		var toUtc = to.HasValue
			? EndOfLocalDateExclusiveUtc(to.Value, zone)
			: DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

		var timeOff = await _repoManager.DoctorScheduling.GetTimeOff(doctorId, fromUtc, toUtc, false);

		return timeOff
			.Select(item => ToTimeOffResponse(item, zone))
			.OrderBy(item => item.Date)
			.ThenBy(item => item.StartTime)
			.ToList();
	}

	public async Task<Result<TimeOffResponse>> CreateTimeOffAsync(
		string doctorId,
		CreateTimeOffRequest request)
	{
		if (!OwnsDoctor(doctorId))
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own time off.");
		}

		if (!await DoctorExistsAsync(doctorId))
		{
			return DoctorNotFound(doctorId);
		}

		if (request.Date == default)
		{
			return Error.Validation("Scheduling.DateRequired", "A date is required.");
		}

		if (request.EndDate.HasValue && request.EndDate.Value < request.Date)
		{
			return Error.Validation(
				"Scheduling.InvalidRange",
				"The end date must not precede the start date.");
		}

		var settings = await _repoManager.DoctorScheduling.GetSettings(doctorId, false)
			?? DefaultSettings(doctorId);
		var zone = ResolveTimeZone(settings.TimeZoneId, out var timeZoneError);
		if (zone is null)
		{
			return timeZoneError!;
		}

		var conversion = ConvertTimeOffRequest(request, zone);
		if (conversion.IsFailure)
		{
			return conversion.Error;
		}

		var (startUtc, endUtc) = conversion.Value;

		// Serialize with booking and with other time-off edits for this doctor.
		await using var bookingLock = await _repoManager.Appointments.AcquireBookingLockAsync(doctorId);

		var existingTimeOff = await _repoManager.DoctorScheduling.GetTimeOff(doctorId, startUtc, endUtc, false);
		if (existingTimeOff.Count > 0)
		{
			return Error.Conflict(
				"Scheduling.TimeOffOverlap",
				"This time off overlaps an existing time-off range. Remove or adjust the existing range first.");
		}

		var activeAppointments = await _repoManager.Appointments
			.GetActiveAppointmentsByDoctorIdInRange(doctorId, startUtc, endUtc, false);
		var conflictingIds = activeAppointments
			.Select(appointment => appointment.Id)
			.Distinct()
			.OrderBy(id => id)
			.ToList();

		if (conflictingIds.Count > 0)
		{
			return Error.Conflict(
				"Scheduling.AppointmentConflict",
				DescribeAppointmentConflicts(
					conflictingIds,
					"this time off",
					"the time off was not added"));
		}

		var timeOff = new DoctorTimeOff
		{
			DoctorId = doctorId,
			StartTimeUtc = startUtc,
			EndTimeUtc = endUtc,
			Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
			CreatedAtUtc = DateTime.UtcNow,
		};

		_repoManager.DoctorScheduling.CreateTimeOff(timeOff);
		await _repoManager.SaveAsync();
		await bookingLock.CommitAsync();

		return ToTimeOffResponse(timeOff, zone);
	}

	public async Task<Result> DeleteTimeOffAsync(string doctorId, int id)
	{
		if (!OwnsDoctor(doctorId))
		{
			return Error.Forbidden("Scheduling.Forbidden", "You can only manage your own time off.");
		}

		var timeOff = await _repoManager.DoctorScheduling.GetTimeOff(id, true);
		if (timeOff is null || timeOff.DoctorId != doctorId)
		{
			return Error.NotFound("Scheduling.TimeOffNotFound", "Time off was not found.");
		}

		_repoManager.DoctorScheduling.DeleteTimeOff(timeOff);
		await _repoManager.SaveAsync();

		return Result.Success();
	}

	public async Task<Result<DoctorAvailabilityResponse>> GetAvailabilityAsync(
		string doctorId,
		DateOnly from,
		DateOnly to)
	{
		if (to < from)
		{
			return Error.Validation("Scheduling.InvalidRange", "The end date must not precede the start date.");
		}

		if (to.DayNumber - from.DayNumber + 1 > MaxAvailabilityRangeDays)
		{
			return Error.Validation(
				"Scheduling.RangeTooLarge",
				$"Availability can be requested for at most {MaxAvailabilityRangeDays} days at a time.");
		}

		if (!await DoctorExistsAsync(doctorId))
		{
			return DoctorNotFound(doctorId);
		}

		var settings = await _repoManager.DoctorScheduling.GetSettings(doctorId, false)
			?? DefaultSettings(doctorId);
		var zone = ResolveTimeZone(settings.TimeZoneId, out var timeZoneError);
		if (zone is null)
		{
			return timeZoneError!;
		}

		var periods = await _repoManager.DoctorScheduling.GetAvailabilityPeriods(doctorId, false);
		var duration = TimeSpan.FromMinutes(settings.ConsultationDurationMinutes);

		var rangeStartUtc = StartOfLocalDateUtc(from, zone);
		var rangeEndUtc = EndOfLocalDateExclusiveUtc(to, zone);

		var timeOff = await _repoManager.DoctorScheduling.GetTimeOff(doctorId, rangeStartUtc, rangeEndUtc, false);
		var appointments = await _repoManager.Appointments
			.GetActiveAppointmentsByDoctorIdInRange(doctorId, rangeStartUtc, rangeEndUtc, false);

		var response = new DoctorAvailabilityResponse
		{
			DoctorId = doctorId,
			TimeZoneId = settings.TimeZoneId,
			ConsultationDurationMinutes = settings.ConsultationDurationMinutes,
			Days = new List<AvailableDayResponse>(),
		};

		for (var date = from; ; date = date.AddDays(1))
		{
			response.Days.Add(new AvailableDayResponse
			{
				Date = date,
				Slots = CalculateSlots(date, periods, timeOff, appointments, duration, zone),
			});

			if (date == to)
			{
				break;
			}
		}

		return response;
	}

	public async Task<Result<TimeSpan>> ValidateBookingTimeAsync(
		string doctorId,
		DateTime startUtc,
		TimeSpan? requestedDuration)
	{
		var doctor = await _repoManager.Doctors.GetById(doctorId, false);
		if (doctor is null)
		{
			return DoctorNotFound(doctorId);
		}

		// IsAvailable is the doctor's own "not taking new requests" switch. A slot
		// grid can still be published while it is off, so the rule belongs on the
		// booking path rather than being left to the client to hide.
		if (!doctor.IsAvailable)
		{
			return Error.Conflict(
				"Scheduling.DoctorUnavailable",
				"This doctor is not accepting new appointment requests at the moment. Please try another doctor or check back later.");
		}

		var settings = await _repoManager.DoctorScheduling.GetSettings(doctorId, false)
			?? DefaultSettings(doctorId);
		var zone = ResolveTimeZone(settings.TimeZoneId, out var timeZoneError);
		if (zone is null)
		{
			return timeZoneError!;
		}

		var duration = TimeSpan.FromMinutes(settings.ConsultationDurationMinutes);

		if (requestedDuration.HasValue && requestedDuration.Value != duration)
		{
			return Error.Validation(
				"Scheduling.DurationMismatch",
				$"This doctor offers {settings.ConsultationDurationMinutes}-minute consultations. The requested duration does not match.");
		}

		var normalizedStartUtc = AsUtc(startUtc);
		if (normalizedStartUtc <= DateTime.UtcNow)
		{
			return Error.Validation("Appointments.StartInPast", "The appointment must start in the future.");
		}

		var endUtc = normalizedStartUtc + duration;
		var localStart = TimeZoneInfo.ConvertTimeFromUtc(normalizedStartUtc, zone);
		var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endUtc, zone);
		var date = DateOnly.FromDateTime(localStart);

		var periods = await _repoManager.DoctorScheduling.GetAvailabilityPeriods(doctorId, false);
		var fitsPeriod = periods
			.Where(period => period.DayOfWeek == date.DayOfWeek)
			.Any(period =>
			{
				var midnight = LocalMidnight(date);
				var periodStart = midnight + period.StartTime;
				var periodEnd = midnight + period.EndTime;

				if (localStart < periodStart || localStart >= periodEnd || localEnd > periodEnd)
				{
					return false;
				}

				var offset = localStart - periodStart;
				return offset.Ticks % duration.Ticks == 0;
			});

		if (!fitsPeriod)
		{
			return Error.Conflict(
				"Scheduling.OutsideWorkingHours",
				"The requested time is not one of the doctor's offered slots.");
		}

		var timeOff = await _repoManager.DoctorScheduling.GetTimeOff(doctorId, normalizedStartUtc, endUtc, false);
		if (timeOff.Count > 0)
		{
			return Error.Conflict("Scheduling.TimeOffConflict", "The doctor is taking time off during the requested time.");
		}

		var appointments = await _repoManager.Appointments
			.GetActiveAppointmentsByDoctorIdInRange(doctorId, normalizedStartUtc, endUtc, false);
		if (appointments.Any())
		{
			return Error.Conflict("Scheduling.AppointmentConflict", "The doctor already has an appointment at the requested time.");
		}

		return duration;
	}

	private static List<AvailableSlotResponse> CalculateSlots(
		DateOnly date,
		IReadOnlyList<DoctorAvailabilityPeriod> periods,
		IReadOnlyList<DoctorTimeOff> timeOff,
		IEnumerable<Appointment> appointments,
		TimeSpan duration,
		TimeZoneInfo zone)
	{
		var slots = new List<AvailableSlotResponse>();
		var seenStarts = new HashSet<DateTime>();

		var dayStartUtc = StartOfLocalDateUtc(date, zone);
		var dayEndUtc = EndOfLocalDateExclusiveUtc(date, zone);
		var nowUtc = DateTime.UtcNow;

		var busy = new List<(DateTime StartUtc, DateTime EndUtc)>();
		busy.AddRange(timeOff
			.Where(item => item.StartTimeUtc < dayEndUtc && item.EndTimeUtc > dayStartUtc)
			.Select(item => (item.StartTimeUtc, item.EndTimeUtc)));
		busy.AddRange(appointments
			.Where(item => item.StartTime < dayEndUtc && item.StartTime + item.Duration > dayStartUtc)
			.Select(item => (item.StartTime, item.StartTime + item.Duration)));

		foreach (var period in periods
			.Where(item => item.DayOfWeek == date.DayOfWeek)
			.OrderBy(item => item.StartTime))
		{
			var midnight = LocalMidnight(date);
			var periodStart = midnight + period.StartTime;
			var periodEnd = midnight + period.EndTime;

			for (var localStart = periodStart;
				localStart + duration <= periodEnd;
				localStart += duration)
			{
				foreach (var startUtc in LocalStartCandidates(localStart, zone))
				{
					// The stored appointment duration is elapsed time, so verify
					// the UTC end still lands inside the same local period. This
					// also keeps a slot from spanning a DST transition.
					var endUtc = startUtc + duration;
					var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endUtc, zone);

					if (startUtc < nowUtc || localEnd > periodEnd)
					{
						continue;
					}

					if (busy.Any(window => startUtc < window.EndUtc && endUtc > window.StartUtc))
					{
						continue;
					}

					if (!seenStarts.Add(startUtc))
					{
						continue;
					}

					slots.Add(new AvailableSlotResponse
					{
						Date = date,
						StartTime = startUtc,
						EndTime = endUtc,
						DurationMinutes = (int)duration.TotalMinutes,
					});
				}
			}
		}

		return slots.OrderBy(slot => slot.StartTime).ToList();
	}

	private static bool IsInsideAnyPeriod(
		DateTime startUtc,
		TimeSpan duration,
		IReadOnlyCollection<AvailabilityPeriodRequest> periods,
		TimeZoneInfo zone)
	{
		var normalizedStartUtc = AsUtc(startUtc);
		var localStart = TimeZoneInfo.ConvertTimeFromUtc(normalizedStartUtc, zone);
		var localEnd = TimeZoneInfo.ConvertTimeFromUtc(normalizedStartUtc + duration, zone);
		var date = DateOnly.FromDateTime(localStart);
		var midnight = LocalMidnight(date);

		return periods
			.Where(period => period.DayOfWeek == date.DayOfWeek)
			.Any(period =>
			{
				var periodStart = midnight + period.StartTime;
				var periodEnd = midnight + period.EndTime;
				return localStart >= periodStart && localEnd <= periodEnd;
			});
	}

	private static Result<(DateTime StartUtc, DateTime EndUtc)> ConvertTimeOffRequest(
		CreateTimeOffRequest request,
		TimeZoneInfo zone)
	{
		var lastDate = request.EndDate ?? request.Date;

		if (request.IsFullDay)
		{
			var fullDayStartUtc = StartOfLocalDateUtc(request.Date, zone);
			var fullDayEndUtc = EndOfLocalDateExclusiveUtc(lastDate, zone);
			return (fullDayStartUtc, fullDayEndUtc);
		}

		if (!request.StartTime.HasValue || !request.EndTime.HasValue)
		{
			return Error.Validation(
				"Scheduling.TimeOffTimesRequired",
				"Start time and end time are required unless the whole day is blocked.");
		}

		var startLocal = LocalMidnight(request.Date) + request.StartTime.Value;
		var endLocal = LocalMidnight(lastDate) + request.EndTime.Value;

		if (zone.IsInvalidTime(startLocal) || zone.IsInvalidTime(endLocal))
		{
			return Error.Validation(
				"Scheduling.InvalidTime",
				"That local time does not exist on the selected date because of a daylight-saving transition.");
		}

		// For ambiguous times, block the widest real interval so both
		// occurrences of the repeated local hour are covered.
		var startUtc = ConvertLocalToUtc(startLocal, zone, preferEarliest: true);
		var endUtc = ConvertLocalToUtc(endLocal, zone, preferEarliest: false);

		if (endUtc <= startUtc)
		{
			return Error.Validation("Scheduling.InvalidTimeRange", "The end time must be after the start time.");
		}

		return (startUtc, endUtc);
	}

	private static TimeOffResponse ToTimeOffResponse(DoctorTimeOff timeOff, TimeZoneInfo zone)
	{
		var localStart = TimeZoneInfo.ConvertTimeFromUtc(timeOff.StartTimeUtc, zone);
		var localEnd = TimeZoneInfo.ConvertTimeFromUtc(timeOff.EndTimeUtc, zone);
		var endDate = DateOnly.FromDateTime(localEnd);
		var endsAtMidnight = localEnd.TimeOfDay == TimeSpan.Zero && localEnd > localStart;
		var isFullDay = localStart.TimeOfDay == TimeSpan.Zero && endsAtMidnight;

		// The end of a full-day (or midnight-ending) range is exclusive, so the
		// last unavailable calendar date is the day before the UTC end.
		if (endsAtMidnight)
		{
			endDate = endDate.AddDays(-1);
		}

		return new TimeOffResponse
		{
			Id = timeOff.Id,
			Date = DateOnly.FromDateTime(localStart),
			EndDate = endDate,
			StartTime = localStart.TimeOfDay,
			EndTime = localEnd.TimeOfDay,
			IsFullDay = isFullDay,
			Reason = timeOff.Reason,
			// SQL Server stores DateTime without a kind. The value is UTC by
			// contract, so mark it as such or System.Text.Json would omit the
			// trailing "Z" and clients could interpret it in their local zone.
			StartTimeUtc = DateTime.SpecifyKind(timeOff.StartTimeUtc, DateTimeKind.Utc),
			EndTimeUtc = DateTime.SpecifyKind(timeOff.EndTimeUtc, DateTimeKind.Utc),
		};
	}

	private static DoctorScheduleResponse BuildScheduleResponse(
		string doctorId,
		DoctorSchedulingSettings settings,
		IEnumerable<DoctorAvailabilityPeriod> periods)
	{
		return new DoctorScheduleResponse
		{
			DoctorId = doctorId,
			TimeZoneId = settings.TimeZoneId,
			ConsultationDurationMinutes = settings.ConsultationDurationMinutes,
			Periods = periods
				.OrderBy(period => period.DayOfWeek)
				.ThenBy(period => period.StartTime)
				.Select(period => new AvailabilityPeriodResponse
				{
					Id = period.Id,
					DayOfWeek = period.DayOfWeek,
					StartTime = period.StartTime,
					EndTime = period.EndTime,
				})
				.ToList(),
		};
	}

	private static Error? ValidatePeriods(IReadOnlyCollection<AvailabilityPeriodRequest>? periods)
	{
		if (periods is null)
		{
			return Error.Validation(
				"Scheduling.PeriodsRequired",
				"Availability periods are required. Send an empty list for a fully unavailable week.");
		}

		var maximumEnd = TimeSpan.FromHours(24);

		foreach (var period in periods)
		{
			if (!Enum.IsDefined(period.DayOfWeek))
			{
				return Error.Validation("Scheduling.InvalidWeekday", "Each period must use a valid day of the week.");
			}

			if (period.StartTime < TimeSpan.Zero || period.StartTime >= maximumEnd)
			{
				return Error.Validation("Scheduling.InvalidPeriod", "A period must start within its weekday.");
			}

			if (period.EndTime <= period.StartTime || period.EndTime >= maximumEnd)
			{
				return Error.Validation(
					"Scheduling.InvalidPeriod",
					"A period must end after it starts and before midnight.");
			}
		}

		foreach (var group in periods.GroupBy(period => period.DayOfWeek))
		{
			var ordered = group.OrderBy(period => period.StartTime).ToList();
			for (var index = 1; index < ordered.Count; index++)
			{
				if (ordered[index].StartTime < ordered[index - 1].EndTime)
				{
					return Error.Validation(
						"Scheduling.OverlappingPeriods",
						$"Availability periods for {group.Key} overlap or are duplicated.");
				}
			}
		}

		return null;
	}

	private static string DescribeAppointmentConflicts(
		IReadOnlyCollection<int> appointmentIds,
		string change,
		string notApplied)
	{
		var shown = appointmentIds.Take(20).ToList();
		var remainder = appointmentIds.Count - shown.Count;
		var ids = string.Join(", ", shown) + (remainder > 0 ? $", and {remainder} more" : string.Empty);

		return $"{appointmentIds.Count} active appointment(s) conflict with {change} (IDs: {ids}). "
			+ $"Reschedule or cancel them first; {notApplied}.";
	}

	private static TimeZoneInfo? ResolveTimeZone(string? timeZoneId, out Error? error)
	{
		error = null;

		if (string.IsNullOrWhiteSpace(timeZoneId))
		{
			error = Error.Validation("Scheduling.InvalidTimeZone", "A time zone is required.");
			return null;
		}

		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
		}
		catch (TimeZoneNotFoundException)
		{
			error = Error.Validation(
				"Scheduling.InvalidTimeZone",
				$"Time zone '{timeZoneId}' is not known on the server.");
			return null;
		}
		catch (InvalidTimeZoneException)
		{
			error = Error.Validation(
				"Scheduling.InvalidTimeZone",
				$"Time zone '{timeZoneId}' is not usable on the server.");
			return null;
		}
	}

	private static DateTime LocalMidnight(DateOnly date)
	{
		return DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
	}

	/// <summary>The earliest UTC instant that belongs to the local calendar date.</summary>
	private static DateTime StartOfLocalDateUtc(DateOnly date, TimeZoneInfo zone)
	{
		var midnight = LocalMidnight(date);

		// A few zones skip midnight itself on a DST transition, so walk forward
		// to the first local time that exists on that date.
		for (var minutes = 0; minutes <= 24 * 60; minutes++)
		{
			var candidate = midnight.AddMinutes(minutes);
			if (!zone.IsInvalidTime(candidate))
			{
				return ConvertLocalToUtc(candidate, zone, preferEarliest: true);
			}
		}

		throw new InvalidOperationException($"No valid local time exists on {date:yyyy-MM-dd} in {zone.Id}.");
	}

	private static DateTime EndOfLocalDateExclusiveUtc(DateOnly date, TimeZoneInfo zone)
	{
		return date == DateOnly.MaxValue
			? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
			: StartOfLocalDateUtc(date.AddDays(1), zone);
	}

	private static DateTime ConvertLocalToUtc(DateTime local, TimeZoneInfo zone, bool preferEarliest)
	{
		local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

		if (zone.IsInvalidTime(local))
		{
			throw new ArgumentException("The local time does not exist in this time zone.", nameof(local));
		}

		if (zone.IsAmbiguousTime(local))
		{
			var offsets = zone.GetAmbiguousTimeOffsets(local);
			var offset = preferEarliest ? offsets.Max() : offsets.Min();
			return DateTime.SpecifyKind(local - offset, DateTimeKind.Utc);
		}

		return TimeZoneInfo.ConvertTimeToUtc(local, zone);
	}

	private static IEnumerable<DateTime> LocalStartCandidates(DateTime local, TimeZoneInfo zone)
	{
		local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

		if (zone.IsInvalidTime(local))
		{
			yield break;
		}

		if (zone.IsAmbiguousTime(local))
		{
			foreach (var offset in zone.GetAmbiguousTimeOffsets(local).Distinct())
			{
				yield return DateTime.SpecifyKind(local - offset, DateTimeKind.Utc);
			}

			yield break;
		}

		yield return TimeZoneInfo.ConvertTimeToUtc(local, zone);
	}

	private static DateTime AsUtc(DateTime value)
	{
		return value.Kind switch
		{
			DateTimeKind.Utc => value,
			DateTimeKind.Local => value.ToUniversalTime(),
			_ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
		};
	}

	private static DoctorSchedulingSettings DefaultSettings(string doctorId)
	{
		return new DoctorSchedulingSettings
		{
			DoctorId = doctorId,
			TimeZoneId = DefaultTimeZoneId,
			ConsultationDurationMinutes = DoctorSchedulingSettings.DefaultConsultationDurationMinutes,
		};
	}

	private async Task<bool> DoctorExistsAsync(string doctorId)
	{
		return await _repoManager.Doctors.GetById(doctorId, false) is not null;
	}

	private bool OwnsDoctor(string doctorId)
	{
		return string.Equals(_userClaimsService.GetUserId(), doctorId, StringComparison.Ordinal);
	}

	private static Error DoctorNotFound(string doctorId)
	{
		return Error.NotFound("Doctors.NotFound", $"doctor with id [{doctorId}] isn't found");
	}
}
