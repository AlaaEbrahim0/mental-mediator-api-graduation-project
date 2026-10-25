namespace Domain.Entities;

public class DoctorSchedulingSettings
{
	public string DoctorId { get; set; } = string.Empty;
	public Doctor Doctor { get; set; } = null!;
	public string TimeZoneId { get; set; } = "UTC";
	public int SlotIncrementMinutes { get; set; } = 30;
	public int BufferBeforeMinutes { get; set; }
	public int BufferAfterMinutes { get; set; }
	public int MinimumNoticeMinutes { get; set; } = 30;
	public int MaxBookingDaysAhead { get; set; } = 90;
}
