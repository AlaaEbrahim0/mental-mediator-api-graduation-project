namespace Domain.Entities;

/// <summary>
/// One 1-5 star rating of a doctor by a patient account. The unique
/// (DoctorId, RaterUserId) pair is the rule: a second submission edits the
/// existing row instead of creating another vote.
/// </summary>
public class DoctorRating
{
	public int Id { get; set; }

	public string DoctorId { get; set; } = string.Empty;

	public string RaterUserId { get; set; } = string.Empty;

	/// <summary>Rater's display name captured at submit time, like chat members.</summary>
	public string DisplayName { get; set; } = string.Empty;

	public int Stars { get; set; }

	public string? Comment { get; set; }

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? UpdatedAtUtc { get; set; }
}
