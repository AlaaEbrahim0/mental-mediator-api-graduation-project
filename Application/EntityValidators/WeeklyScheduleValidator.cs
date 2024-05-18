using Domain.Entities;
using FluentValidation;

namespace Application.EntityValidators;

public class WeeklyScheduleValidator : AbstractValidator<DoctorScheduleWeekDay>
{
	public WeeklyScheduleValidator()
	{
		RuleFor(x => x.DoctorId)
			.NotEmpty();

		RuleFor(x => x.StartTime)
			.NotEmpty()
			.LessThan(x => x.EndTime)
			.WithMessage("Start time must be less than end time");

		RuleFor(x => x.SessionDuration)
			.NotEmpty()
			.LessThanOrEqualTo(x => new TimeSpan(1, 30, 0))
			.WithMessage("Session duration must be less than or equal to 30 minutes");

		RuleFor(x => x.DayOfWeek)
			.NotEmpty();


		RuleFor(x => x.EndTime)
			.NotEmpty();
	}

}