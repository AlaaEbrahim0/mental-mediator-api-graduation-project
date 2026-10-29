using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Utilities;

/// <summary>
/// Choosing which portal an OAuth handoff returns to, and turning a server-side
/// failure into something safe to put in a browser URL.
/// </summary>
public static class AuthHandoff
{
	public const string PatientSetting = "ClientUI";
	public const string DoctorSetting = "DoctorUI";

	/// <summary>Catch-all code for any failure we do not describe to the browser.</summary>
	public const string GenericFailureCode = "external_failed";

	// Code -> the only string allowed in the redirect. Unknown codes collapse to
	// the generic one, so a new error cannot leak its description cross-origin.
	private static readonly IReadOnlyDictionary<string, string> PublicCodes =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["ExternalAuthentication.RoleUnsupported"] = "role_unsupported",
			["ExternalAuthentication.EmailUnverified"] = "email_unverified",
			["ExternalAuthentication.ProfileUnavailable"] = "profile_unavailable",
			["ExternalAuthentication.AccountIncomplete"] = "account_incomplete",
			["ExternalAuthentication.IdentityTaken"] = "identity_taken",
			["ExternalAuthentication.RemoteError"] = "remote_error",
		};

	/// <summary>
	/// Same rule the password-reset link already uses: a clinician goes to the
	/// clinician portal, everyone else to the patient portal.
	/// </summary>
	public static string SettingForRoles(IEnumerable<string> roles) =>
		roles.Contains("Doctor", StringComparer.OrdinalIgnoreCase)
			? DoctorSetting
			: PatientSetting;

	/// <summary>
	/// Configured portal origin, falling back to the request origin when the
	/// setting is blank. The fallback is logged because a missing value silently
	/// redirects users to the API host, which looks like a broken login.
	/// </summary>
	public static string ResolveBaseUrl(
		IConfiguration configuration,
		HttpRequest request,
		string settingName,
		ILogger? logger = null)
	{
		var configured = configuration[settingName];

		if (!string.IsNullOrWhiteSpace(configured))
		{
			return configured.TrimEnd('/');
		}

		var origin = $"{request.Scheme}://{request.Host}";
		logger?.LogWarning(
			"{Setting} is not configured; the sign-in handoff falls back to the request origin {Origin}",
			settingName,
			origin);

		return origin;
	}

	/// <summary>
	/// Maps an error to a code that is safe to expose. Only the key is returned —
	/// descriptions for this flow can contain address details, and the redirect is
	/// a top-level navigation whose query string is saved to browser history.
	/// </summary>
	public static string PublicErrorCode(Error error) =>
		error is not null && PublicCodes.TryGetValue(error.Code, out var code)
			? code
			: GenericFailureCode;

	public static string BuildFailureRedirect(string baseUrl, string publicCode) =>
		$"{baseUrl}/?authError={Uri.EscapeDataString(publicCode)}";

	public static string BuildHandoffRedirect(string baseUrl, string temporaryToken) =>
		$"{baseUrl}/?temporaryToken={Uri.EscapeDataString(temporaryToken)}";
}
