using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Utilities;

/// <summary>
/// Chooses portal origin for confirmation and password-reset links.
/// Configured URL is validated before use; caller-supplied destination
/// never trusted.
/// </summary>
public static class AuthHandoff
{
	public const string PatientSetting = "ClientUI";
	public const string DoctorSetting = "DoctorUI";

	/// <summary>
	/// Same rule the password-reset link already uses: a clinician goes to the
	/// clinician portal, everyone else to the patient portal.
	/// </summary>
	public static string SettingForRoles(IEnumerable<string> roles) =>
		roles.Contains("Doctor", StringComparer.OrdinalIgnoreCase)
			? DoctorSetting
			: PatientSetting;

	/// <summary>
	/// Reads a configured portal origin and returns it only when it is an
	/// absolute HTTP(S) URL without credentials, query or fragment. Returning
	/// null is deliberate: callers that put this value in a browser redirect
	/// must not fall back to the request Host, which is attacker-controlled.
	/// </summary>
	public static string? TryResolveConfiguredBaseUrl(
		IConfiguration configuration,
		string settingName,
		ILogger? logger = null)
	{
		var configured = configuration[settingName]?.Trim();

		if (string.IsNullOrWhiteSpace(configured))
		{
			logger?.LogWarning(
				"{Setting} is not configured; no portal redirect will be offered",
				settingName);
			return null;
		}

		if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri) ||
			(uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) ||
			!string.IsNullOrEmpty(uri.UserInfo) ||
			!string.IsNullOrEmpty(uri.Query) ||
			!string.IsNullOrEmpty(uri.Fragment))
		{
			logger?.LogWarning(
				"{Setting} is not a safe absolute portal URL; no portal redirect will be offered",
				settingName);
			return null;
		}

		return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
	}

	/// <summary>
	/// Configured portal origin, falling back to the request origin when the
	/// setting is blank or invalid. Confirmation and reset links need
	/// a base URL on the same request; the fallback is logged because it silently points
	/// users at the API host, which looks like a broken login.
	/// </summary>
	public static string ResolveBaseUrl(
		IConfiguration configuration,
		HttpRequest request,
		string settingName,
		ILogger? logger = null)
	{
		var configured = TryResolveConfiguredBaseUrl(configuration, settingName);

		if (configured is not null)
		{
			return configured;
		}

		var origin = $"{request.Scheme}://{request.Host}";
		logger?.LogWarning(
			"{Setting} did not provide a safe portal origin; the handoff falls back to the request origin {Origin}",
			settingName,
			origin);

		return origin;
	}

}
