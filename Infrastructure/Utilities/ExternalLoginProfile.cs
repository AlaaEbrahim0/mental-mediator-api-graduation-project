using System.Security.Claims;

namespace Infrastructure.Utilities;

/// <summary>
/// The account details the app needs from a Google identity.
/// LastName and PhotoUrl are optional; FirstName is never null because the
/// column is required by the model.
/// </summary>
public sealed record ExternalLoginProfile(
	string Email,
	string FirstName,
	string? LastName,
	string? PhotoUrl,
	bool EmailVerified);

/// <summary>
/// Reads an <see cref="ExternalLoginProfile"/> out of the external principal.
/// </summary>
/// <remarks>
/// Every claim is looked up under more than one name on purpose. The OIDC handler
/// applies a legacy inbound claim map, so <c>given_name</c>, <c>family_name</c>
/// and <c>name</c> can arrive either as the original OIDC type or as a
/// <see cref="ClaimTypes"/> URI depending on configuration, and
/// <c>family_name</c> is historically mapped onto <c>ClaimTypes.Name</c> rather
/// than a surname type. Reading a single spelling is what made the old code
/// dereference a null email.
/// </remarks>
public static class ExternalLoginProfileReader
{
	private static readonly string[] EmailClaims =
	[
		"email",
		ClaimTypes.Email
	];

	// ClaimTypes.Name is intentionally NOT treated as an email: under the legacy
	// map it can hold the display name.
	// The second spelling is what JsonWebTokenHandler's default inbound map turns
	// "email_verified" into when claim mapping is left on, which the external
	// cookie path does independently of the JWT bearer configuration.
	private static readonly string[] VerifiedClaims =
	[
		"email_verified",
		"http://schemas.xmlsoap.org/ws/2005/09/identity/claims/emailverified"
	];

	private static readonly string[] PhotoClaims =
	[
		"picture",
		"profilepic"
	];

	private static readonly string[] GivenNameClaims =
	[
		"given_name",
		ClaimTypes.GivenName
	];

	private static readonly string[] FamilyNameClaims =
	[
		"family_name",
		ClaimTypes.Surname
	];

	private static readonly string[] FullNameClaims =
	[
		"name",
		ClaimTypes.Name
	];

	/// <summary>
	/// Returns null when the principal carries no usable email address, so the
	/// caller can fail with a typed error instead of throwing inside the OAuth
	/// callback.
	/// </summary>
	public static ExternalLoginProfile? TryRead(ClaimsPrincipal? principal)
	{
		if (principal is null)
		{
			return null;
		}

		var email = FirstValue(principal, EmailClaims);
		if (string.IsNullOrWhiteSpace(email))
		{
			return null;
		}

		email = email.Trim();

		var given = FirstValue(principal, GivenNameClaims);
		var family = FirstValue(principal, FamilyNameClaims);

		if (string.IsNullOrWhiteSpace(given) && string.IsNullOrWhiteSpace(family))
		{
			(given, family) = SplitName(FirstValue(principal, FullNameClaims));
		}

		// FirstName maps to a required column, so an absent Google display name
		// falls back to the local part of the address rather than a null write.
		var firstName = TrimmedOr(family: given, fallback: email.Split('@')[0]);
		var lastName = string.IsNullOrWhiteSpace(family) ? null : family.Trim();

		return new ExternalLoginProfile(
			email,
			firstName,
			lastName,
			TrimmedOrNull(FirstValue(principal, PhotoClaims)),
			IsVerified(principal));
	}

	/// <summary>
	/// Claim types present on the principal. Used only in the log line emitted
	/// when a profile cannot be read, so a claim-mapping surprise is diagnosable
	/// from the server log instead of a blind retry.
	/// </summary>
	public static string DescribeClaims(ClaimsPrincipal? principal) =>
		principal is null
			? "(no principal)"
			: string.Join(", ", principal.Claims
				.Select(claim => claim.Type)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(type => type, StringComparer.Ordinal));

	private static bool IsVerified(ClaimsPrincipal principal)
	{
		foreach (var type in VerifiedClaims)
		{
			var claim = principal.FindFirst(type);
			if (claim is null)
			{
				continue;
			}

			// Absent or unparsable means unverified: Google always sends the
			// claim for the email scope, so guessing "true" here would weaken the
			// only evidence the app has that the address belongs to the caller.
			if (bool.TryParse(claim.Value, out var verified))
			{
				return verified;
			}

			return string.Equals(claim.Value, "true", StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}

	private static (string? Given, string? Family) SplitName(string? fullName)
	{
		if (string.IsNullOrWhiteSpace(fullName))
		{
			return (null, null);
		}

		var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return parts.Length switch
		{
			1 => (parts[0], null),
			_ => (parts[0], string.Join(' ', parts.Skip(1))),
		};
	}

	private static string? FirstValue(ClaimsPrincipal principal, IEnumerable<string> types) =>
		types
			.Select(type => principal.FindFirstValue(type))
			.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

	private static string TrimmedOr(string? family, string fallback) =>
		string.IsNullOrWhiteSpace(family) ? fallback : family.Trim();

	private static string? TrimmedOrNull(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
