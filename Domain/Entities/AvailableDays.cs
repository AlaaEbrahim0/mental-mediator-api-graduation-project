using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;


public class AvailableDays
{
	public int Id { get; set; }

	[Required]
	public DayOfWeek DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan SessionDuration { get; set; }
	public TimeSpan EndTime { get; set; }
	public int? WeeklyScheduleId { get; set; }

	//public WeeklySchedule? WeeklySchedule { get; set; }
}

