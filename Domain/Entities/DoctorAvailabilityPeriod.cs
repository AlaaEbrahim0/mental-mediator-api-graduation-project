namespace Domain.Entities;

/// <summary>
/// One recurring working period in the doctor's local wall-clock time.
/// A weekday may have several periods (for example 09:00-12:00 and
/// 13:00-17:00); a weekday with none is a non-working day.
/// </summary>
public class DoctorAvailabilityPeriod
{
	public int Id { get; set; }

	public string DoctorId { get; set; } = string.Empty;

	public Doctor Doctor { get; set; } = null!;

	public DayOfWeek DayOfWeek { get; set; }

	public TimeSpan StartTime { get; set; }

	public TimeSpan EndTime { get; set; }
}
