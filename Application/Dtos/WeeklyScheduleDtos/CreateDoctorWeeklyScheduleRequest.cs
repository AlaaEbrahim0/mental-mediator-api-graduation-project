using FluentValidation;

namespace Application.Dtos.WeeklyScheduleDtos;


public class CreateDoctorWeeklyScheduleRequest
{
	public List<CreateScheduleWeekDayRequest> WeekDays { get; set; } = new();
}

public class CreateDoctorWeeklyScheduleRequestValidator : AbstractValidator<CreateDoctorWeeklyScheduleRequest>
{
	public CreateDoctorWeeklyScheduleRequestValidator()
	{
		RuleFor(x => x.WeekDays)
			.NotEmpty()
			.WithMessage("Available days cannot be empty")
			.Must(x => x.Count <= 7)
			.WithMessage("Available days cannot exceed 7 days")
			.Must(x => x.Select(x => x.DayOfWeek).Distinct().Count() == x.Select(x => x.DayOfWeek).Count())
			.WithMessage("Available days must not have duplicate days");
	}
}