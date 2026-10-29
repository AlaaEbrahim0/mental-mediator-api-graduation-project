namespace Application.Options;

/// <summary>
/// Configuration for the Resend transactional email provider.
/// The API key is a secret and must be supplied via environment variable
/// or deployment secret — never hard-coded. The sender address and display
/// name are public configuration.
/// </summary>
public class ResendSettings
{
	/// <summary>Resend API key (RESEND_API_KEY). Never logged.</summary>
	public string? ApiKey { get; set; }

	/// <summary>
	/// Verified sending domain / email address, e.g. "nexus@your-domain.com".
	/// Must be a domain claimed and verified in the Resend dashboard.
	/// </summary>
	public string? FromEmail { get; set; }

	/// <summary>Optional display name shown beside the From address.</summary>
	public string? FromName { get; set; }

	/// <inheritdoc />
	public bool IsConfigured =>
		!string.IsNullOrWhiteSpace(ApiKey) &&
		!string.IsNullOrWhiteSpace(FromEmail);
}
