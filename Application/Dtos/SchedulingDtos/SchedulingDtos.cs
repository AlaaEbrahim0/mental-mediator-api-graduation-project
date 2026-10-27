using FluentValidation;

namespace Application.Dtos.SchedulingDtos;

public class AvailabilityPeriodRequest
{
	public DayOfWeek DayOfWeek { get; set; }

	public TimeSpan StartTime { get; set; }

	public TimeSpan EndTime { get; set; }
}

public class UpdateDoctorScheduleRequest
{
	public string TimeZoneId { get; set; } = "UTC";

	public int ConsultationDurationMinutes { get; set; } = 30;

	public List<AvailabilityPeriodRequest> Periods { get; set; } = new();
}

public class AvailabilityPeriodResponse
{
	public int Id { get; set; }
	public DayOfWeek DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan EndTime { get; set; }
}

public class DoctorScheduleResponse
{
	public string DoctorId { get; set; } = string.Empty;
	public string TimeZoneId { get; set; } = "UTC";
	public int ConsultationDurationMinutes { get; set; } = 30;
	public List<AvailabilityPeriodResponse> Periods { get; set; } = new();
}

public class CreateTimeOffRequest
{
	/// <summary>First local calendar date in the doctor's configured time zone.</summary>
	public DateOnly Date { get; set; }

	/// <summary>
	/// Last local calendar date, inclusive. Omit for a single date. For a
	/// partial-day range the same times are used on the first and last date,
	/// and the whole interval between them is blocked.
	/// </summary>
	public DateOnly? EndDate { get; set; }

	/// <summary>Ignored when <see cref="IsFullDay"/> is true.</summary>
	public TimeSpan? StartTime { get; set; }

	/// <summary>Ignored when <see cref="IsFullDay"/> is true.</summary>
	public TimeSpan? EndTime { get; set; }

	public bool IsFullDay { get; set; }

	public string? Reason { get; set; }
}

public class TimeOffResponse
{
	public int Id { get; set; }
	public DateOnly Date { get; set; }
	public DateOnly EndDate { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan EndTime { get; set; }
	public bool IsFullDay { get; set; }
	public string? Reason { get; set; }
	public DateTime StartTimeUtc { get; set; }
	public DateTime EndTimeUtc { get; set; }
}

public class AvailableSlotResponse
{
	public DateOnly Date { get; set; }
	public DateTime StartTime { get; set; }
	public DateTime EndTime { get; set; }
	public int DurationMinutes { get; set; }
}

public class AvailableDayResponse
{
	public DateOnly Date { get; set; }
	public List<AvailableSlotResponse> Slots { get; set; } = new();
}

public class DoctorAvailabilityResponse
{
	public string DoctorId { get; set; } = string.Empty;
	public string TimeZoneId { get; set; } = "UTC";
	public int ConsultationDurationMinutes { get; set; }
	public List<AvailableDayResponse> Days { get; set; } = new();
}

public class UpdateDoctorScheduleRequestValidator : AbstractValidator<UpdateDoctorScheduleRequest>
{
	public UpdateDoctorScheduleRequestValidator()
	{
		RuleFor(x => x.TimeZoneId)
			.NotEmpty().WithMessage("Time zone is required.")
			.Must(BeAKnownTimeZone)
			.WithMessage("Time zone must be a valid IANA or Windows time zone id.");

		RuleFor(x => x.ConsultationDurationMinutes)
			.Must(x => x is 30 or 45 or 60)
			.WithMessage("Consultation duration must be 30, 45, or 60 minutes.");

		RuleFor(x => x.Periods)
			.NotNull()
			.WithMessage("Periods are required; use an empty list for a fully unavailable week.");

		RuleForEach(x => x.Periods).ChildRules(period =>
		{
			period.RuleFor(x => x.DayOfWeek)
				.IsInEnum()
				.WithMessage("Weekday must be a valid day of the week.");

			period.RuleFor(x => x.StartTime)
				.GreaterThanOrEqualTo(TimeSpan.Zero)
				.WithMessage("Start time cannot be negative.");

			period.RuleFor(x => x.EndTime)
				.GreaterThan(x => x.StartTime)
				.WithMessage("End time must be greater than start time.")
				.LessThan(TimeSpan.FromHours(24))
				.WithMessage("End time must be before midnight.");
		});

		RuleFor(x => x.Periods).Custom((periods, context) =>
		{
			if (periods is null || periods.Count == 0)
			{
				return;
			}

			foreach (var group in periods.GroupBy(period => period.DayOfWeek))
			{
				var ordered = group.OrderBy(period => period.StartTime).ToList();
				for (var index = 1; index < ordered.Count; index++)
				{
					if (ordered[index].StartTime < ordered[index - 1].EndTime)
					{
						context.AddFailure(
							nameof(UpdateDoctorScheduleRequest.Periods),
							$"Availability periods for {group.Key} overlap.");
						break;
					}
				}
			}
		});
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

public class CreateTimeOffRequestValidator : AbstractValidator<CreateTimeOffRequest>
{
	public CreateTimeOffRequestValidator()
	{
		RuleFor(x => x.Date)
			.NotEmpty()
			.WithMessage("A date is required.");

		RuleFor(x => x.EndDate)
			.Must((request, endDate) => !endDate.HasValue || endDate.Value >= request.Date)
			.WithMessage("End date must not precede the start date.");

		RuleFor(x => x.Reason)
			.MaximumLength(500)
			.WithMessage("Reason must not exceed 500 characters.");

		When(x => !x.IsFullDay, () =>
		{
			RuleFor(x => x.StartTime)
				.NotNull().WithMessage("Start time is required unless the whole day is blocked.");

			RuleFor(x => x.EndTime)
				.NotNull().WithMessage("End time is required unless the whole day is blocked.");

			// A range can end at an earlier wall-clock time on a later date, for
			// example 17:00 on Monday through 09:00 on Wednesday. Only a
			// single-date partial day must end after it starts.
			RuleFor(x => x.EndTime)
				.GreaterThan(x => x.StartTime)
				.WithMessage("End time must be greater than start time.")
				.When(x => x.StartTime.HasValue && x.EndTime.HasValue
					&& (!x.EndDate.HasValue || x.EndDate.Value == x.Date));

			RuleFor(x => x.StartTime)
				.Must(x => x >= TimeSpan.Zero && x < TimeSpan.FromHours(24))
				.WithMessage("Start time must be within the selected day.");

			RuleFor(x => x.EndTime)
				.Must(x => x > TimeSpan.Zero && x <= TimeSpan.FromHours(24))
				.WithMessage("End time must be within the selected day.");
		});
	}
}
