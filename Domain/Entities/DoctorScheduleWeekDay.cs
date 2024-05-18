using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class DoctorScheduleWeekDay
{
	public string? DoctorId { get; set; }
	public DayOfWeek DayOfWeek { get; set; }

	public Doctor? Doctor { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan SessionDuration { get; set; }
	public TimeSpan EndTime { get; set; }
}

[NotMapped]
public class WeeklySchedule
{
	public WeeklySchedule(List<DoctorScheduleWeekDay> days)
	{
		WeekDays = days;
	}
	private List<DoctorScheduleWeekDay> WeekDays { get; set; } = new();
}
