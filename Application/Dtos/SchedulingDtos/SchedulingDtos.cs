using Domain.Enums;
using FluentValidation;

namespace Application.Dtos.SchedulingDtos;

/// <summary>
/// Recurring weekly break, expressed in the doctor's wall-clock time so it can
/// be compared against the working hours stored on <c>DoctorScheduleWeekDay</c>
/// without a timezone conversion on every slot.
/// </summary>
public class CreateRecurringBreakRequest
{
	public DayOfWeek DayOfWeek { get; set; }

	public TimeSpan StartTime { get; set; }

	public TimeSpan EndTime { get; set; }

	public string? Name { get; set; }
}

public class RecurringBreakResponse
{
	public int Id { get; set; }
	public DayOfWeek DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan EndTime { get; set; }
	public string? Name { get; set; }
	public bool IsActive { get; set; }
}

public class CreateRecurringBreakRequestValidator : AbstractValidator<CreateRecurringBreakRequest>
{
	public CreateRecurringBreakRequestValidator()
	{
		RuleFor(x => x.EndTime)
			.GreaterThan(x => x.StartTime)
			.WithMessage("End time must be greater than start time.");

		RuleFor(x => x.Name)
			.MaximumLength(200)
			.WithMessage("Name must not exceed 200 characters.");
	}
}

/// <summary>
/// A date-specific availability change. <see cref="Type"/> decides how the
/// override interacts with the doctor's normal working hours for that date.
/// </summary>
public class CreateAvailabilityOverrideRequest
{
	public DateOnly Date { get; set; }

	public DoctorAvailabilityOverrideType Type { get; set; }

	/// <summary>
	/// Wall-clock window for UnavailableInterval / ReplaceWorkingHours /
	/// AdditionalWorkingSession. Ignored for FullDayLeave.
	/// </summary>
	public TimeSpan? StartTime { get; set; }

	public TimeSpan? EndTime { get; set; }

	public string? PrivateReason { get; set; }
}

public class AvailabilityOverrideResponse
{
	public int Id { get; set; }
	public DateOnly Date { get; set; }
	public DoctorAvailabilityOverrideType Type { get; set; }
	public TimeSpan? StartTime { get; set; }
	public TimeSpan? EndTime { get; set; }
	public string? PrivateReason { get; set; }
	public bool IsActive { get; set; }
}

public class CreateAvailabilityOverrideRequestValidator : AbstractValidator<CreateAvailabilityOverrideRequest>
{
	public CreateAvailabilityOverrideRequestValidator()
	{
		RuleFor(x => x.Type)
			.IsInEnum()
			.WithMessage("Override type must be a valid value.");

		RuleFor(x => x)
			.Must(HaveWindowForWindowedTypes)
			.When(x => x.Type is DoctorAvailabilityOverrideType.UnavailableInterval
				or DoctorAvailabilityOverrideType.ReplaceWorkingHours
				or DoctorAvailabilityOverrideType.AdditionalWorkingSession)
			.WithMessage("Start time and end time are required for this override type.");

		RuleFor(x => x.EndTime)
			.GreaterThan(x => x.StartTime)
			.When(x => x.StartTime.HasValue && x.EndTime.HasValue)
			.WithMessage("End time must be greater than start time.");

		RuleFor(x => x.PrivateReason)
			.MaximumLength(500)
			.WithMessage("Reason must not exceed 500 characters.");
	}

	private static bool HaveWindowForWindowedTypes(CreateAvailabilityOverrideRequest request)
	{
		return request.StartTime.HasValue && request.EndTime.HasValue;
	}
}

/// <summary>
/// A one-off block of time in UTC. UTC is used because a block can cross a
/// date boundary and the doctor's wall-clock zone is per-doctor.
/// </summary>
public class CreateTimeBlockRequest
{
	public DateTime StartTimeUtc { get; set; }

	public DateTime EndTimeUtc { get; set; }

	public DoctorTimeBlockType Type { get; set; }

	public string? PrivateReason { get; set; }
}

public class TimeBlockResponse
{
	public int Id { get; set; }
	public DateTime StartTimeUtc { get; set; }
	public DateTime EndTimeUtc { get; set; }
	public DoctorTimeBlockType Type { get; set; }
	public string? PrivateReason { get; set; }
	public bool IsCancelled { get; set; }
}

public class CreateTimeBlockRequestValidator : AbstractValidator<CreateTimeBlockRequest>
{
	public CreateTimeBlockRequestValidator()
	{
		RuleFor(x => x.EndTimeUtc)
			.GreaterThan(x => x.StartTimeUtc)
			.WithMessage("End time must be greater than start time.");

		RuleFor(x => x.Type)
			.IsInEnum()
			.WithMessage("Block type must be a valid value.");

		RuleFor(x => x.PrivateReason)
			.MaximumLength(500)
			.WithMessage("Reason must not exceed 500 characters.");
	}
}

/// <summary>
/// Doctor-level scheduling defaults. The slot calculator falls back to the
/// <see cref="DoctorSchedulingSettings"/> defaults when a doctor has no row.
/// </summary>
public class UpdateSchedulingSettingsRequest
{
	public string TimeZoneId { get; set; } = "UTC";

	public int SlotIncrementMinutes { get; set; } = 30;

	public int BufferBeforeMinutes { get; set; }

	public int BufferAfterMinutes { get; set; }

	public int MinimumNoticeMinutes { get; set; } = 30;

	public int MaxBookingDaysAhead { get; set; } = 90;
}

public class SchedulingSettingsResponse
{
	public string DoctorId { get; set; } = string.Empty;
	public string TimeZoneId { get; set; } = "UTC";
	public int SlotIncrementMinutes { get; set; }
	public int BufferBeforeMinutes { get; set; }
	public int BufferAfterMinutes { get; set; }
	public int MinimumNoticeMinutes { get; set; }
	public int MaxBookingDaysAhead { get; set; }
}

public class UpdateSchedulingSettingsRequestValidator : AbstractValidator<UpdateSchedulingSettingsRequest>
{
	public UpdateSchedulingSettingsRequestValidator()
	{
		RuleFor(x => x.TimeZoneId)
			.NotEmpty()
			.WithMessage("Time zone is required.")
			.Must(BeAKnownTimeZone)
			.WithMessage("Time zone must be a valid IANA or Windows time zone id.");

		RuleFor(x => x.SlotIncrementMinutes)
			.InclusiveBetween(5, 120)
			.WithMessage("Slot increment must be between 5 and 120 minutes.");

		RuleFor(x => x.BufferBeforeMinutes)
			.InclusiveBetween(0, 120)
			.WithMessage("Buffer before must be between 0 and 120 minutes.");

		RuleFor(x => x.BufferAfterMinutes)
			.InclusiveBetween(0, 120)
			.WithMessage("Buffer after must be between 0 and 120 minutes.");

		RuleFor(x => x.MinimumNoticeMinutes)
			.InclusiveBetween(0, 1440)
			.WithMessage("Minimum notice must be between 0 and 1440 minutes.");

		RuleFor(x => x.MaxBookingDaysAhead)
			.InclusiveBetween(1, 730)
			.WithMessage("Max booking window must be between 1 and 730 days.");
	}

	private static bool BeAKnownTimeZone(string timeZoneId)
	{
		if (string.IsNullOrWhiteSpace(timeZoneId))
		{
			return false;
		}

		try
		{
			TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
			return true;
		}
		catch (TimeZoneNotFoundException)
		{
			return false;
		}
		catch (InvalidTimeZoneException)
		{
			return false;
		}
	}
}
