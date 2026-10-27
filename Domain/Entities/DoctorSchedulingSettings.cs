namespace Domain.Entities;

/// <summary>
/// Doctor-level scheduling configuration: the time zone used to interpret
/// weekly periods and the single consultation duration offered to patients.
/// A missing row means "UTC, 30 minutes".
/// </summary>
public class DoctorSchedulingSettings
{
	public const int DefaultConsultationDurationMinutes = 30;

	public string DoctorId { get; set; } = string.Empty;

	public Doctor Doctor { get; set; } = null!;

	public string TimeZoneId { get; set; } = "UTC";

	public int ConsultationDurationMinutes { get; set; } = DefaultConsultationDurationMinutes;
}
