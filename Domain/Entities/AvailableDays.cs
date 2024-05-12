namespace Domain.Entities;

public class AvailableDays
{
	public int Id { get; set; }
	public DayOfWeek? DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan SessionDuration { get; set; } = new TimeSpan(0, 30, 0);

	public TimeSpan EndTime { get; set; }

	public int? WeeklyScheduleId { get; set; }
	//public WeeklySchedule? WeeklySchedule { get; set; }
}

