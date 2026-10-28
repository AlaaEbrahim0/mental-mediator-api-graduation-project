namespace Domain.Entities;

/// <summary>
/// A degree or formal qualification. Stored separately from the legacy
/// <see cref="Doctor.Qualifications"/> free-text summary so profiles can show
/// a structured education timeline.
/// </summary>
public class UserQualification
{
	public int Id { get; set; }

	public string UserId { get; set; } = string.Empty;

	public BaseUser User { get; set; } = null!;

	public string Degree { get; set; } = string.Empty;

	public string Institution { get; set; } = string.Empty;

	public string? FieldOfStudy { get; set; }

	public int? StartYear { get; set; }

	public int? EndYear { get; set; }

	public string? Description { get; set; }
}
