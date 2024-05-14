using Domain.Entities;
using FluentValidation;

namespace Application.EntityValidators;

public class WeeklyScheduleValidator : AbstractValidator<WeeklySchedule>
{
	public WeeklyScheduleValidator()
	{
		RuleFor(x => x.DoctorId)
			.NotEmpty();
	}
}