using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A promised check-in on a patient. "Pending follow-ups" on the dashboard is
/// Status == Open; DueAtUtc is optional because some follow-ups are
/// "when convenient" rather than dated.
/// </summary>
public class FollowUp
{
	public int Id { get; set; }

	public string DoctorId { get; set; } = string.Empty;

	public string PatientId { get; set; } = string.Empty;

	public int? AppointmentId { get; set; }

	public string Reason { get; set; } = string.Empty;

	public FollowUpStatus Status { get; set; } = FollowUpStatus.Open;

	public DateTime? DueAtUtc { get; set; }

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? ResolvedAtUtc { get; set; }
}
