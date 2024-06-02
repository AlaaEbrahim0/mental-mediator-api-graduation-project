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

