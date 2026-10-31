using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Application.Contracts;
using Application.Dtos.AuthDtos;
using Application.Dtos.NotificationDtos;
using Application.Utilities;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Infrastructure.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared;

namespace Infrastructure.Services;
public class AuthService : IAuthService
{
	/// <summary>The only role this portal's Google sign-up is allowed to mint.</summary>
	private const string PatientRole = "User";

	private readonly UserManager<BaseUser> _userManager;
	private readonly RoleManager<IdentityRole> _roleManager;

	private readonly SignInManager<BaseUser> _signInManager;
	private readonly JwtTokenGenerator _jwtTokenGenerator;
	private readonly MailTemplates _MailTemplates;
	private readonly IMailService _mailService;
	private readonly IMapper _mapper;
	private readonly IConfiguration _configuration;
	private readonly ILogger<AuthService> _logger;

	public AuthService(
		UserManager<BaseUser> userManager,
		RoleManager<IdentityRole> roleManager,
		SignInManager<BaseUser> signInManager,
		JwtTokenGenerator jwtTokenGenerator,
		IMailService mailService,
		IMapper mapper,
		MailTemplates MailTemplates,
		IConfiguration configuration,
		ILogger<AuthService> logger)
	{
		_userManager = userManager;
		_roleManager = roleManager;
		_mapper = mapper;
		_signInManager = signInManager;
		_jwtTokenGenerator = jwtTokenGenerator;
		_mailService = mailService;
		_MailTemplates = MailTemplates;
		_configuration = configuration;
		_logger = logger;
	}

	public async Task<Result<AuthResponse>> SignInAsync(SignInRequest signInModel)
	{
		var authModel = new AuthResponse();

		var user = await _userManager.FindByEmailAsync(signInModel.Email);

		if (user is null || !await _userManager.CheckPasswordAsync(user, signInModel.Password))
		{
			return UserErrors.InvalidCredentials();
		}

		if (!await _userManager.IsEmailConfirmedAsync(user))
		{
			return UserErrors.EmailNotConfirmed();
		}

		var token = await _jwtTokenGenerator.CreateJwtToken(user);
		var userRoles = await _userManager.GetRolesAsync(user!);

		authModel.Token = new JwtSecurityTokenHandler().WriteToken(token);
		authModel.ExpiresOn = token.ValidTo;
		authModel.Email = user.Email;
		authModel.UserName = user.FullName;
		authModel.PhotoUrl = user.PhotoUrl;
		authModel.Roles = userRoles.ToList();
		authModel.Message = "User was authenticated successfully";
		authModel.UserId = user.Id;

		return authModel;
	}

	public async Task<Result<EmailConfirmationResponse>> SendEmailConfirmationLink(string email)
	{
		// Always return the same success message regardless of whether the
		// email belongs to an account. Revealing "user not found" vs "email
		// already confirmed" lets an attacker enumerate valid addresses.
		var user = await _userManager.FindByEmailAsync(email);
		if (user is null || user.EmailConfirmed)
		{
			return new EmailConfirmationResponse
			{
				Email = email,
				Message = "An email confirmation link has been sent to the provided email address"
			};
		}

		var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
		var confirmationLink = generateEmailConfirmationLink(user.Id, token);
		var emailContent = _MailTemplates.EmailConfirmation(user.FullName, confirmationLink);

		var mailResult = await _mailService.SendEmailAsync(new MailRequest()
		{
			ToEmail = user.Email,
			Subject = emailContent.Subject,
			Body = emailContent.Html,
			TextBody = emailContent.Text
		});

		// The confirmation link only reaches the user through this email, so a
		// delivery failure is fatal for the request and has to be reported.
		if (mailResult.IsFailure)
		{
			_logger.LogWarning(
				"Failed to send email confirmation link to user {UserId}: {Error}",
				user.Id, mailResult.Error.Description);
		}

		// Return the same success message even when delivery failed, to avoid
		// leaking whether the account exists. The server-side log captures the
		// delivery error for operators.
		return new EmailConfirmationResponse
		{
			Email = email,
			Message = "An email confirmation link has been sent to the provided email address"
		};
	}

	private string generateEmailConfirmationLink(string id, string token)
	{
		// A fragment is not sent in the HTTP request, so neither reverse-proxy
		// logs nor the initial navigation record the token. confirm-email.html
		// removes it from history and POSTs it to the API.
		return
			$"{_signInManager.Context.Request.Scheme}://" +
			$"{_signInManager.Context.Request.Host}" +
			$"/confirm-email.html#" +
			$"id={WebUtility.UrlEncode(id)}&" +
			$"token={WebUtility.UrlEncode(token)}";
	}

	/// <summary>
	/// Both portals expose /forgetpassword/resetpassword, so the link is pointed at
	/// the portal the account actually belongs to. The doctor host was previously
	/// hardcoded, which sent every patient through the doctor sign-in screen, and
	/// the scheme was taken from the request while the host was a literal, so a
	/// plain-HTTP hop produced a broken mixed-scheme link.
	/// </summary>
	private async Task<string> GenerateResetPasswordLinkAsync(BaseUser user, string token)
	{
		var roles = await _userManager.GetRolesAsync(user);
		var baseUrl = AuthHandoff.ResolveBaseUrl(
			_configuration,
			_signInManager.Context.Request,
			AuthHandoff.SettingForRoles(roles),
			_logger);

		return
			$"{baseUrl}/forgetpassword/resetpassword?" +
			$"email={WebUtility.UrlEncode(user.Email)}&" +
			$"token={WebUtility.UrlEncode(token)}";
	}

	public async Task<Result<RegisterationResponse>> RegisterAsync(RegisterationRequest request)
	{
		var user = await _userManager.FindByEmailAsync(request.Email!);

		if (user is not null)
		{
			return UserErrors.EmailNotUnique(request.Email);
		}

		user = UserFactory.CreateUser(request, _mapper);

		var createUserResult = await _userManager.CreateAsync(user, request.Password);

		var sb = new StringBuilder();
		if (!createUserResult.Succeeded)
		{
			foreach (var error in createUserResult.Errors)
			{
				sb.AppendLine(error.Code + " : " + error.Description);
			}
			return UserErrors.ValidationErrors(sb.ToString());
		}

		await _userManager.AddToRoleAsync(user, request.Role);
		var EmailConfirmationResult = await SendEmailConfirmationLink(user.Email!);

		if (EmailConfirmationResult.IsFailure)
		{
			return EmailConfirmationResult.Error;
		}

		var response = new RegisterationResponse();
		response.Message = $"User: [{request.Email}] has been created successfully, please confirm your email address to continue.";

		return response;
	}

	public async Task<Result<AuthResponse>> ExternalLoginAsync()
	{
		var externalUserInfo = await _signInManager.GetExternalLoginInfoAsync();
		if (externalUserInfo is null)
		{
			return ExternalAuthenticationErrors.RemoteError();
		}

		var profile = ExternalLoginProfileReader.TryRead(externalUserInfo.Principal);
		if (profile is null)
		{
			// The claim list goes to the log, never to the client: a Google claim
			// mapping change otherwise shows up as an unexplained "try again".
			_logger.LogWarning(
				"Google principal carried no usable email address. Claim types: {Claims}",
				ExternalLoginProfileReader.DescribeClaims(externalUserInfo.Principal));

			return ExternalAuthenticationErrors.ProfileUnavailable();
		}

		// The linked login wins over the address: a Google account's email can be
		// changed by its owner, its subject cannot.
		var user = await _userManager.FindByLoginAsync(
				externalUserInfo.LoginProvider,
				externalUserInfo.ProviderKey)
			?? await _userManager.FindByEmailAsync(profile.Email);

		if (user is null)
		{
			return await CreateExternalUserAsync(profile, externalUserInfo);
		}

		// Refuse a clinician before any token is minted. This path provisions
		// patients; a Doctor token handed to the patient portal would let
		// /api/notifications and the notification hub stream clinical data into
		// the wrong UI, and no Doctor row would exist if we created one here.
		var roles = await _userManager.GetRolesAsync(user);
		if (roles.Count == 0)
		{
			return ExternalAuthenticationErrors.AccountIncomplete();
		}

		if (!roles.Contains(PatientRole, StringComparer.OrdinalIgnoreCase))
		{
			return ExternalAuthenticationErrors.RoleUnsupported();
		}

		await EnsureExternalLinkAsync(user, externalUserInfo);

		// Google asserted ownership of this address, which is also what unlocks an
		// account that registered but never clicked the confirmation link.
		if (!user.EmailConfirmed && profile.EmailVerified)
		{
			user.EmailConfirmed = true;
			var confirmed = await _userManager.UpdateAsync(user);
			if (!confirmed.Succeeded)
			{
				LogIdentityFailure("confirming email for an existing Google sign-in", confirmed);
			}
		}

		return await BuildAuthResponseAsync(user, signedIn: true);
	}

	/// <summary>
	/// Creates the patient account a first-time Google sign-in implies, and only
	/// reports success once the account is complete: persisted, role-bearing and
	/// linked to the external identity.
	/// </summary>
	private async Task<Result<AuthResponse>> CreateExternalUserAsync(
		ExternalLoginProfile profile,
		ExternalLoginInfo externalUserInfo)
	{
		// Creating is the one place an unverified address matters: it would hand
		// whoever controls this Google account a new identity at that address.
		if (!profile.EmailVerified)
		{
			_logger.LogWarning(
				"Google sign-in refused because email_verified was not true for a new account");
			return ExternalAuthenticationErrors.EmailUnverified();
		}

		var candidate = new User
		{
			Email = profile.Email,
			// UserName is overridden as a getter derived from Email, but the base
			// setter still has to be filled: NormalizedUserName reads it, and it
			// null-forgives, so leaving it unset throws inside CreateAsync.
			UserName = profile.Email.Split('@')[0],
			FirstName = profile.FirstName,
			LastName = profile.LastName ?? string.Empty,
			PhotoUrl = profile.PhotoUrl,
			// Set before CreateAsync so it lands in the same write. The old code
			// assigned it afterwards without an Update call, so it never persisted
			// and the account could never use password sign-in.
			EmailConfirmed = true,
		};

		var created = await _userManager.CreateAsync(candidate);
		if (!created.Succeeded)
		{
			LogIdentityFailure("creating the account for a Google sign-in", created);
			return MapCreationFailure(created);
		}

		// Identity throws InvalidOperationException for an unknown role name rather
		// than returning a failed result, and these roles only exist because
		// scripts/seed.sql inserted them. Checking first keeps a database that never
		// ran the seed on the owned failure path instead of 500-ing in the middle of
		// a browser redirect and leaving a role-less account behind.
		if (!await _roleManager.RoleExistsAsync(PatientRole))
		{
			_logger.LogWarning(
				"Cannot complete a Google sign-up: the {Role} role does not exist in this database, " +
				"so scripts/seed.sql has not been run",
				PatientRole);

			await AbandonNewAccount(candidate, "a database without the patient role");
			return ExternalAuthenticationErrors.AccountIncomplete();
		}

		var roleAdded = await _userManager.AddToRoleAsync(candidate, PatientRole);
		if (!roleAdded.Succeeded)
		{
			LogIdentityFailure("assigning the patient role to a Google sign-up", roleAdded);
			await AbandonNewAccount(candidate, "a failed role assignment");
			return ExternalAuthenticationErrors.AccountIncomplete();
		}

		await EnsureExternalLinkAsync(candidate, externalUserInfo);

		return await BuildAuthResponseAsync(candidate, signedIn: false);
	}

	/// <summary>
	/// Removes an account that was created but could not be completed, so a
	/// refused sign-up never leaves a user that no portal will accept.
	/// </summary>
	private async Task AbandonNewAccount(BaseUser user, string reason)
	{
		var deleted = await _userManager.DeleteAsync(user);
		if (!deleted.Succeeded)
		{
			// The row is still there and needs manual cleanup; say so with the id.
			_logger.LogError(
				"Could not remove the half-built account {UserId} after {Reason}: {Errors}",
				user.Id,
				reason,
				string.Join("; ", deleted.Errors.Select(error => $"{error.Code}:{error.Description}")));
		}
	}

	/// <summary>
	/// Records the external identity so the next sign-in resolves by subject.
	/// A failure here is not fatal: address matching still works.
	/// </summary>
	private async Task EnsureExternalLinkAsync(BaseUser user, ExternalLoginInfo externalUserInfo)
	{
		var existing = await _userManager.FindByLoginAsync(
			externalUserInfo.LoginProvider,
			externalUserInfo.ProviderKey);

		if (existing is not null)
		{
			return;
		}

		var linked = await _userManager.AddLoginAsync(user, externalUserInfo);
		if (!linked.Succeeded)
		{
			LogIdentityFailure("linking the Google identity to an account", linked);
		}
	}

	private static Error MapCreationFailure(IdentityResult result)
	{
		var codes = result.Errors.Select(error => error.Code).ToHashSet(StringComparer.Ordinal);

		// DuplicateUserName is reachable: UserName is the email's local part, and
		// john@gmail.com after john@outlook.com collides on "john".
		return codes.Contains("DuplicateUserName") || codes.Contains("DuplicateEmail")
			? ExternalAuthenticationErrors.IdentityTaken()
			: ExternalAuthenticationErrors.AccountIncomplete();
	}

	private async Task<AuthResponse> BuildAuthResponseAsync(BaseUser user, bool signedIn)
	{
		var token = await _jwtTokenGenerator.CreateJwtToken(user);
		var roles = await _userManager.GetRolesAsync(user);

		return new AuthResponse
		{
			Email = user.Email,
			UserId = user.Id,
			UserName = user.FullName,
			PhotoUrl = user.PhotoUrl,
			Token = new JwtSecurityTokenHandler().WriteToken(token),
			ExpiresOn = token.ValidTo,
			Roles = roles.ToList(),
			Message = signedIn
				? "Signed in with Google"
				: "Account created with Google",
		};
	}

	private void LogIdentityFailure(string action, IdentityResult result)
	{
		_logger.LogWarning(
			"Identity operation failed while {Action}: {Errors}",
			action,
			string.Join("; ", result.Errors.Select(error => $"{error.Code}:{error.Description}")));
	}


	public AuthenticationProperties GetExternalAuthenticationProperties(string provider, string? redirectUrl)
	{
		return _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
	}

	public async Task<Result<EmailConfirmationResponse>> ConfirmEmailAsync(string id, string token)
	{
		var user = await _userManager.FindByIdAsync(id);

		if (user is null)
		{
			return UserErrors.NotFound(id);
		}

		if (user.EmailConfirmed)
		{
			return UserErrors.EmailAlreadyConfirmed(user.Email!);
		}

		var result = await _userManager.ConfirmEmailAsync(user, token);

		if (!result.Succeeded)
		{
			var sb = new StringBuilder();
			foreach (var error in result.Errors)
			{
				sb.AppendLine(error.Description);
			}
			return UserErrors.InvalidToken(sb.ToString());
		}

		var roles = await _userManager.GetRolesAsync(user);
		var portalBaseUrl = AuthHandoff.TryResolveConfiguredBaseUrl(
			_configuration,
			AuthHandoff.SettingForRoles(roles),
			_logger);

		return new EmailConfirmationResponse()
		{
			Email = user.Email,
			Message = "Email has been confirmed successfully",
			// Only a validated configured origin is returned. When the setting
			// is missing or unsafe the page shows success without redirecting,
			// so a bad Host header can never become an open redirect.
			RedirectUrl = portalBaseUrl is null ? null : $"{portalBaseUrl}/login?confirmed=1"
		};
	}

	public async Task<Result<string>> SendResetPasswordLink(string email)
	{
		// Always return the same success message regardless of whether the
		// account exists. Enumerating which addresses have accounts is a
		// known pre-authentication attack vector.
		var user = await _userManager.FindByEmailAsync(email);
		if (user is null)
		{
			return "An email with reset password link has been sent to your email address";
		}

		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		var resetPasswordLink = await GenerateResetPasswordLinkAsync(user, token);
		var emailContent = _MailTemplates.ResetPassword(resetPasswordLink);

		var mailResult = await _mailService.SendEmailAsync(new MailRequest
		{
			Subject = emailContent.Subject,
			ToEmail = email,
			Body = emailContent.Html,
			TextBody = emailContent.Text
		});

		// The reset link only reaches the user through this email, so a delivery
		// failure is fatal for the request and has to be reported.
		if (mailResult.IsFailure)
		{
			_logger.LogWarning(
				"Failed to send password reset link to user {UserId}: {Error}",
				user.Id, mailResult.Error.Description);
		}

		return "An email with reset password link has been sent to your email address";
	}

	public async Task<Result<string>> ResetPassword(ResetPasswordRequest request)
	{
		var user = await _userManager.FindByEmailAsync(request.Email);
		if (user is null)
		{
			// Return the same generic error so a reset for a non-existent
			// account cannot be distinguished from a malformed token.
			return UserErrors.InvalidToken("Invalid password reset token or email.");
		}

		var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

		var sb = new StringBuilder();
		if (!result.Succeeded)
		{
			foreach (var error in result.Errors)
			{
				sb.AppendLine(error.Code + " : " + error.Description);
			}
			return UserErrors.ValidationErrors(sb.ToString());
		}

		return "Your password has been reset successfully";
	}
}
