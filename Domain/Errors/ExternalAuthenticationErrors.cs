using Shared;

namespace Domain.Errors;

/// <summary>
/// Failure codes for the Google sign-in path. Descriptions here are deliberately
/// free of email addresses and identity details: the callback is a top-level
/// browser navigation, and only the code is carried across in the query string
/// (see Infrastructure.Utilities.AuthHandoff), because a query string ends up in
/// browser history.
/// </summary>
public static class ExternalAuthenticationErrors
{
	public static Error RemoteError() => Error.Validation(
		"ExternalAuthentication.RemoteError",
		"Sign-in with Google could not be completed. Please try again.");

	public static Error ProfileUnavailable() => Error.Validation(
		"ExternalAuthentication.ProfileUnavailable",
		"Google did not return the account details needed to sign you in.");

	public static Error EmailUnverified() => Error.Failure(
		"ExternalAuthentication.EmailUnverified",
		"Please verify this email address with Google before signing in.");

	/// <summary>
	/// Clinician and admin identities are refused on this path. Google sign-up
	/// provisions a patient account only; a Doctor row needs profile data and
	/// availability periods that an external profile cannot supply, and a token
	/// issued here would open the patient shell with clinician data.
	/// </summary>
	public static Error RoleUnsupported() => Error.Forbidden(
		"ExternalAuthentication.RoleUnsupported",
		"This account is registered as a clinician or administrator and must sign in with a password.");

	public static Error AccountIncomplete() => Error.Failure(
		"ExternalAuthentication.AccountIncomplete",
		"The account could not be prepared for sign-in. Please sign in with a password or try again.");

	public static Error IdentityTaken() => Error.Conflict(
		"ExternalAuthentication.IdentityTaken",
		"An account already exists for this email address. Sign in with your password to link Google.");
}
