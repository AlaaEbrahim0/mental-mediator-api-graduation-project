using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class TimeSlot
{
	public int Id { get; set; }
	public DayOfWeek? DayOfWeek { get; set; }



	public List<StartEndSlot> StartEndSlots { get; set; } = new();

	public int? WeeklyScheduleId { get; set; }
	public WeeklySchedule? WeeklySchedule { get; set; }
}

public class StartEndSlot
{
	public int Id { get; set; }
	public TimeSlot? TimeSlot { get; set; }
	public TimeSpan StartTime { get; set; }

	[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
	public TimeSpan EndTime { get; set; }

	public string Location { get; set; } = string.Empty;
	public bool IsReserved { get; set; }
}