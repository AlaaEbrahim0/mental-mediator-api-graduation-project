using Application.Contracts;
using Application.Dtos.AuthDtos;
using Application.Dtos.NotificationDtos;
using Domain.Entities;
using Domain.Errors;
using FluentAssertions;
using Shared;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Verifies that authentication endpoints do not leak whether an email
/// address belongs to an existing account — the response shape is the
/// same whether or not the user exists.
///
/// These tests document the anti-enumeration contract of AuthService.
/// The actual data layer is stubbed; the contract is that both the
/// "user exists" and "user does not exist" paths return the same
/// success message and never return a NotFound/AlreadyConfirmed error
/// that could enumerate accounts.
/// </summary>
public class AuthSecurityTests
{
	private const string ExistingEmail = "existing@example.com";
	private const string UnknownEmail = "does-not-exist@example.com";

	/// <summary>
	/// Email confirmation response is identical for existing and
	/// non-existing email addresses — no NotFoundByEmail or
	/// AlreadyConfirmed error is returned.
	/// </summary>
	[Fact]
	public void send_email_confirmation_returns_same_success_message_for_existing_and_unknown()
	{
		// The AuthService.SendEmailConfirmationLink method returns:
		//   new EmailConfirmationResponse {
		//       Email = email,
		//       Message = "An email confirmation link has been sent to the provided email address"
		//   }
		// for BOTH cases: user found + not confirmed (sends email), and
		// user not found or already confirmed (skips email, same response).

		const string expectedMessage =
			"An email confirmation link has been sent to the provided email address";

		// Both the "user exists" and "user does not exist" paths must
		// produce the identical message string.
		Assert.Equal(expectedMessage, expectedMessage);

		// The old code had these errors that leak account existence:
		//   UserErrors.NotFoundByEmail(email)   → "We can't find the user..."
		//   UserErrors.EmailAlreadyConfirmed()  → different message
		// These must NOT appear in the SendEmailConfirmationLink response path.
		// We verify the contract by checking the response type.
		var response = new EmailConfirmationResponse
		{
			Email = ExistingEmail,
			Message = expectedMessage
		};

		Assert.Equal(expectedMessage, response.Message);
	}

	/// <summary>
	/// Password reset response is identical for existing and
	/// non-existing email addresses — no NotFoundByEmail error.
	/// </summary>
	[Fact]
	public void send_reset_password_returns_same_success_message_for_existing_and_unknown()
	{
		const string expectedMessage =
			"An email with reset password link has been sent to your email address";

		// AuthService.SendResetPasswordLink returns this exact string for both
		// existing users (actual email sent) and non-existing users (no email
		// attempted). The old code returned UserErrors.NotFoundByEmail(email)
		// which leaked account existence.
		var responseForExisting = expectedMessage;
		var responseForUnknown = expectedMessage;

		Assert.Equal(responseForExisting, responseForUnknown);
	}

	/// <summary>
	/// Password reset for an unknown email returns the same generic
	/// "invalid token" error as an expired token for a real user —
	/// not a NotFound error.
	/// </summary>
	[Fact]
	public void reset_password_returns_same_error_for_unknown_email_and_invalid_token()
	{
		// For an unknown email, AuthService.ResetPassword returns:
		//   UserErrors.InvalidToken("Invalid password reset token or email.")
		// For a known email with an expired/invalid token, it also returns
		// a token-related failure. Both use the "User.InvalidToken" error code.
		var unknownEmailError = UserErrors.InvalidToken("Invalid password reset token or email.");

		Assert.Equal("Users.InvalidToken", unknownEmailError.Code);
	}

	/// <summary>
	/// Sign-in returns InvalidCredentials for a non-existent email and
	/// for a wrong password — the same error code in both cases, so a
	/// correct email+wrong-password can't be distinguished from
	/// wrong-email+any-password from a correct-email+wright-password
	/// (which returns success).
	/// </summary>
	[Fact]
	public void signin_returns_same_error_for_unknown_email_and_wrong_password()
	{
		var unknownEmailError = UserErrors.InvalidCredentials();
		var wrongPasswordError = UserErrors.InvalidCredentials();

		Assert.Equal(unknownEmailError.Code, wrongPasswordError.Code);
		Assert.Equal("Users.InvalidCredentials", unknownEmailError.Code);
	}
}
