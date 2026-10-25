using Domain.Enums;

namespace Domain.Entities;

public class DoctorAvailabilityOverride
{
	public int Id { get; set; }
	public string DoctorId { get; set; } = string.Empty;
	public Doctor Doctor { get; set; } = null!;
	public DateOnly Date { get; set; }
	public DoctorAvailabilityOverrideType Type { get; set; }
	public TimeSpan? StartTime { get; set; }
	public TimeSpan? EndTime { get; set; }
	public string? PrivateReason { get; set; }
	public bool IsActive { get; set; } = true;
}
