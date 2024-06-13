namespace Domain.Entities;

public class DoctorScheduleWeekDay
{
	public string DoctorId { get; set; } = null!;
	public Doctor Doctor { get; set; } = null!;

	public DayOfWeek DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan SessionDuration { get; set; }
	public TimeSpan EndTime { get; set; }
}

