using FluentValidation;

namespace Application.Dtos.AppointmentDtos;
public class CreateAppointmentRequest
{
	public DateTime StartTime { get; set; }
	public TimeSpan Duration { get; set; }
	public string? Location { get; set; }
	public string? Reason { get; set; }
	public decimal Fees { get; set; }
}

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
	public CreateAppointmentRequestValidator()
	{
		RuleFor(x => x.StartTime)
			.NotEmpty().WithMessage("Start time is required.")
			.Must(BeAValidDate).WithMessage("Start time must be a valid date.")
			.Must(x => x > DateTime.UtcNow);

		RuleFor(x => x.Duration)
			.GreaterThan(TimeSpan.Zero).WithMessage("Duration must be greater than zero.");

		RuleFor(x => x.Location)
			.MaximumLength(200).WithMessage("Location must not exceed 200 characters.")
			.NotEmpty().WithMessage("Location is required.");

		RuleFor(x => x.Reason)
			.MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.");
		RuleFor(x => x.Duration).LessThanOrEqualTo(TimeSpan.FromHours(8));

		RuleFor(x => x.Fees)
			.GreaterThanOrEqualTo(0).WithMessage("Fees must be greater than or equal to zero.");
	}

	private bool BeAValidDate(DateTime date)
	{
		return !date.Equals(default(DateTime));
	}
}


