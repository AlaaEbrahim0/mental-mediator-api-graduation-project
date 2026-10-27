namespace Domain.Entities;

/// <summary>
/// A one-off date/time range during which the doctor is unavailable.
/// The instants are stored in UTC so they compare directly with appointments.
/// </summary>
public class DoctorTimeOff
{
	public int Id { get; set; }

	public string DoctorId { get; set; } = string.Empty;

	public Doctor Doctor { get; set; } = null!;

	public DateTime StartTimeUtc { get; set; }

	public DateTime EndTimeUtc { get; set; }

	public string? Reason { get; set; }

	public DateTime CreatedAtUtc { get; set; }
}
