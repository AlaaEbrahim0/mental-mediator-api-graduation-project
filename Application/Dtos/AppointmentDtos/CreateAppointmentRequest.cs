using FluentValidation;

namespace Application.Dtos.AppointmentDtos;

/// <summary>
/// A patient's booking request.
///
/// Only <see cref="StartTime"/> and <see cref="Location"/> are needed. The
/// duration and fee are owned by the doctor's configuration: a duration may be
/// sent for compatibility but it must match the doctor's consultation
/// duration, and any fee is ignored. <see cref="StartTime"/> is a UTC instant.
/// </summary>
public class CreateAppointmentRequest
{
	public DateTime StartTime { get; set; }

	/// <summary>
	/// Optional. When supplied it must equal the doctor's configured duration;
	/// the server always stores the configured duration.
	/// </summary>
	public TimeSpan? Duration { get; set; }

	public string? Location { get; set; }

	public string? Reason { get; set; }

	/// <summary>Ignored by the server; the appointment fee comes from the doctor profile.</summary>
	public decimal? Fees { get; set; }
}

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
	public CreateAppointmentRequestValidator()
	{
		RuleFor(x => x.StartTime)
			.NotEmpty().WithMessage("Start time is required.")
			.Must(BeAValidDate).WithMessage("Start time must be a valid date.")
			.Must(startTime => NormalizeToUtc(startTime) > DateTime.UtcNow)
			.WithMessage("Start time must be in the future.");

		When(x => x.Duration.HasValue, () =>
		{
			RuleFor(x => x.Duration!.Value)
				.GreaterThan(TimeSpan.Zero).WithMessage("Duration must be greater than zero.")
				.LessThanOrEqualTo(TimeSpan.FromHours(8));
		});

		RuleFor(x => x.Location)
			.MaximumLength(200).WithMessage("Location must not exceed 200 characters.")
			.NotEmpty().WithMessage("Location is required.");

		RuleFor(x => x.Reason)
			.MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");

		When(x => x.Fees.HasValue, () =>
		{
			RuleFor(x => x.Fees!.Value)
				.GreaterThanOrEqualTo(0).WithMessage("Fees must be greater than or equal to zero.");
		});
	}

	private static bool BeAValidDate(DateTime date)
	{
		return !date.Equals(default(DateTime));
	}

	private static DateTime NormalizeToUtc(DateTime value)
	{
		return value.Kind switch
		{
			DateTimeKind.Utc => value,
			DateTimeKind.Local => value.ToUniversalTime(),
			_ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
		};
	}
}
