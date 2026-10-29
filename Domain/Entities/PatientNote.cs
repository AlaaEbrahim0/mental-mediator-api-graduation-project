namespace Domain.Entities;

/// <summary>
/// A note a doctor records about a patient. Keyed on the patient, not the
/// consultation: notes are also taken after a room conversation or between
/// sessions, so <see cref="AppointmentId"/> is optional.
/// </summary>
public class PatientNote
{
	public int Id { get; set; }

	public string DoctorId { get; set; } = string.Empty;

	public string PatientId { get; set; } = string.Empty;

	public int? AppointmentId { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Body { get; set; } = string.Empty;

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? UpdatedAtUtc { get; set; }
}
