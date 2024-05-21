using FluentValidation;

namespace Domain.Entities;

public class DoctorScheduleWeekDay
{
	public string? DoctorId { get; set; }
	public Doctor? Doctor { get; set; }

	public DayOfWeek DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan SessionDuration { get; set; }
	public TimeSpan EndTime { get; set; }
}

public class WeeklyScheduleDayValidator : AbstractValidator<DoctorScheduleWeekDay>
{
	public WeeklyScheduleDayValidator()
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

