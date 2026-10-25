using Domain.Enums;

namespace Domain.Entities;

public class DoctorTimeBlock
{
	public int Id { get; set; }
	public string DoctorId { get; set; } = string.Empty;
	public Doctor Doctor { get; set; } = null!;
	public DateTime StartTimeUtc { get; set; }
	public DateTime EndTimeUtc { get; set; }
	public DoctorTimeBlockType Type { get; set; }
	public string? PrivateReason { get; set; }
	public DateTime CreatedAtUtc { get; set; }
	public DateTime? UpdatedAtUtc { get; set; }
	public string? CreatedByUserId { get; set; }
	public bool IsCancelled { get; set; }
	public DateTime? CancelledAtUtc { get; set; }
}
