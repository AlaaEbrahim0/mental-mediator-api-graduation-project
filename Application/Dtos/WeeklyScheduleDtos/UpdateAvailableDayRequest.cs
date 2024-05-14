using System.ComponentModel;
using FluentValidation;

namespace Application.Dtos.WeeklyScheduleDtos;

public class UpdateAvailableDayRequestValidator : AbstractValidator<UpdateAvailableDayRequest>
{
	public UpdateAvailableDayRequestValidator()
	{
		RuleFor(x => x.EndTime)
			.NotNull()
			.NotEmpty()
			.GreaterThan(x => x.StartTime)
			.WithMessage("End time must be greater than start time");

		RuleFor(x => x.SessionDuration)
			.Must(x => x <= new TimeSpan(1, 0, 0))
			.WithMessage("Session duration must not exceed 1 hour");

		RuleFor(x => x.StartTime)
			.NotEmpty()
			.NotNull();
	}
}


public class UpdateAvailableDayRequest
{
	[DefaultValue("08:00:00")]
	public TimeSpan StartTime { get; set; }

	[DefaultValue("00:30:00")]
	public TimeSpan SessionDuration { get; set; }

	[DefaultValue("17:00:00")]
	public TimeSpan EndTime { get; set; }
}


