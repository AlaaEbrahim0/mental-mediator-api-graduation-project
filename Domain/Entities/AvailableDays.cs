using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;


public class DoctorAvailableDay
{
	public int Id { get; set; }

	[Required]
	public DayOfWeek DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; } = new TimeSpan(17, 0, 0);
	public TimeSpan SessionDuration { get; set; } = new TimeSpan(0, 30, 0);
	public TimeSpan EndTime { get; set; } = new TimeSpan(21, 0, 0);
	public int? WeeklyScheduleId { get; set; }

	public WeeklySchedule? WeeklySchedule { get; set; }
}

