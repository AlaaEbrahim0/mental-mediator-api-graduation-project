using FluentValidation;

namespace Application.Dtos.WeeklyScheduleDtos;


public class CreateDoctorWeeklyScheduleRequest
{
	public string Location { get; set; } = string.Empty;
	public decimal Fees { get; set; }
	public List<CreateScheduleWeekDayRequest> WeekDays { get; set; } = new();
}

public class CreateDoctorWeeklyScheduleRequestValidator : AbstractValidator<CreateDoctorWeeklyScheduleRequest>
{
	public CreateDoctorWeeklyScheduleRequestValidator()
	{
		RuleFor(x => x.Location)
			.NotEmpty();

		RuleFor(x => x.Fees)
			.NotEmpty()
			.GreaterThanOrEqualTo(1);

		RuleFor(x => x.WeekDays)
			.NotEmpty()
			.WithMessage("Available days cannot be empty")
			.Must(x => x.Count <= 7)
			.WithMessage("Available days cannot exceed 7 days")
			.Must(x => x.Select(wd => wd.DayOfWeek).Distinct().Count() == x.Select(wd => wd.DayOfWeek).Count())
			.WithMessage("Available days must not have duplicate days");

		RuleFor(x => x.WeekDays).Custom((weekDays, context) =>
		{
			foreach (var weekDay in weekDays)
			{
				var weekDayValidator = new CreateScheduleWeekDayRequestValidator();
				var result = weekDayValidator.Validate(weekDay);
				if (!result.IsValid)
				{
					foreach (var error in result.Errors)
					{
						context.AddFailure(error.PropertyName.Replace("WeekDays[].", string.Empty), error.ErrorMessage);
					}
				}
			}
		});
	}
}