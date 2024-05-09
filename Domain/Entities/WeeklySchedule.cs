namespace Domain.Entities;

public class WeeklySchedule
{
	public int Id { get; set; }
	public string? DoctorId { get; set; }
	public Doctor? Doctor { get; set; }
	public List<TimeSlot> TimeSlots { get; set; } = new();
}
