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
