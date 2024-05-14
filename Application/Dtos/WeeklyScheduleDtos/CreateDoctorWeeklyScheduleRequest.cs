using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Application.Dtos.WeeklyScheduleDtos;


public class CreateDoctorWeeklyScheduleRequest
{
	[Required]
	public List<CreateAvailableDayRequest> AvailableDays { get; set; } = new(7);
}

public class CreateDoctorWeeklyScheduleRequestValidator : AbstractValidator<CreateDoctorWeeklyScheduleRequest>
{
	public CreateDoctorWeeklyScheduleRequestValidator()
	{
		RuleFor(x => x.AvailableDays)
			.NotEmpty()
			.WithMessage("Available days cannot be empty")
			.Must(x => x.Count <= 7)
			.WithMessage("Available days cannot exceed 7 days")
			.Must(x => x.Select(x => x.DayOfWeek).Distinct().Count() == x.Select(x => x.DayOfWeek).Count())
			.WithMessage("Available days must not have duplicate days");
	}
}