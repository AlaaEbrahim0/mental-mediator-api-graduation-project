namespace Domain.Entities;

public class DoctorRecurringBreak
{
	public int Id { get; set; }
	public string DoctorId { get; set; } = string.Empty;
	public Doctor Doctor { get; set; } = null!;
	public DayOfWeek DayOfWeek { get; set; }
	public TimeSpan StartTime { get; set; }
	public TimeSpan EndTime { get; set; }
	public string? Name { get; set; }
	public bool IsActive { get; set; } = true;
}
