namespace Domain.Entities;

/// <summary>
/// A professional certificate, licence, or award that belongs to a user or
/// doctor. Certificates are intentionally attached to <see cref="BaseUser"/>
/// so the same profile sections work for every account type.
/// </summary>
public class UserCertificate
{
	public int Id { get; set; }

	public string UserId { get; set; } = string.Empty;

	public BaseUser User { get; set; } = null!;

	public string Title { get; set; } = string.Empty;

	public string IssuingOrganization { get; set; } = string.Empty;

	public DateOnly IssueDate { get; set; }

	public DateOnly? ExpiryDate { get; set; }

	public string? CredentialId { get; set; }

	public string? CredentialUrl { get; set; }

	public string? Description { get; set; }

	/// <summary>
	/// Optional image of the certificate (uploaded through Cloudinary) or an
	/// externally hosted scan URL.
	/// </summary>
	public string? ImageUrl { get; set; }
}
