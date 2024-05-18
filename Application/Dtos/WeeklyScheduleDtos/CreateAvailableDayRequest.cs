using FluentValidation;

namespace Application.Dtos.WeeklyScheduleDtos;


public class CreateScheduleWeekDayRequest : UpdateScheduleWeekDayRequest
{
	public DayOfWeek DayOfWeek { get; set; }
}

public class CreateScheduleWeekDayRequestValidator : AbstractValidator<CreateScheduleWeekDayRequest>
{
	public CreateScheduleWeekDayRequestValidator()
	{
		RuleFor(x => x.DayOfWeek)
			.NotNull();
	}
}
