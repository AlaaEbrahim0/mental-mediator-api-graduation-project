using API.Configurations;
using Application.Contracts;
using Application.Dtos.AuthDtos;
using Domain.Entities;
using Infrastructure.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;

namespace API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
	private readonly IAuthService _authService;
	private readonly SignInManager<BaseUser> _signInManager;
	private readonly IConfiguration _configuration;
	private readonly IMemoryCache _cache;
	private readonly ILogger<AuthController> _logger;

	public AuthController(
		IAuthService authService,
		SignInManager<BaseUser> signInManager,
		IMemoryCache cache,
		IConfiguration configuration,
		ILogger<AuthController> logger)
	{
		_authService = authService;
		_signInManager = signInManager;
		_cache = cache;
		_configuration = configuration;
		_logger = logger;
	}

	[HttpPost("register")]
	[EnableRateLimiting(RateLimitPolicies.Auth)]
	public async Task<IActionResult> Register([FromBody] RegisterationRequest model)
	{
		if (model is null)
		{
			return UnprocessableEntity(ModelState);
		}
		var result = await _authService.RegisterAsync(model);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return StatusCode(201, result.Value);
	}

	[HttpPost("signin")]
	[EnableRateLimiting(RateLimitPolicies.Auth)]
	public async Task<IActionResult> SignIn([FromBody] SignInRequest model)
	{
		if (model is null)
		{
			return UnprocessableEntity(ModelState);
		}
		var result = await _authService.SignInAsync(model);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("external-login")]
	[EnableRateLimiting(RateLimitPolicies.OAuthHandoff)]
	public IActionResult ExternalLogin()
	{
		var provider = "Google";
		var scheme = Request.Scheme;
		var host = Request.Host.Value;
		var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { }, scheme, host);

		var properties = _authService.GetExternalAuthenticationProperties(provider, redirectUrl);

		return Challenge(properties, provider);
	}

	[HttpGet("external-login-callback")]
	[EnableRateLimiting(RateLimitPolicies.OAuthHandoff)]
	public async Task<IActionResult> ExternalLoginCallback()
	{
		var result = await _authService.ExternalLoginAsync();

		// This action is reached by a redirect from Google, so its "response" is a
		// browser navigation: a ProblemDetails body would be shown as raw JSON.
		// Send the user back to a portal with a code and let the portal own the
		// wording — failure descriptions can contain an email address, and a query
		// string in a top-level navigation is saved to browser history.
		if (result.IsFailure)
		{
			var failedPortal = AuthHandoff.ResolveBaseUrl(
				_configuration, Request, AuthHandoff.PatientSetting, _logger);

			_logger.LogInformation(
				"Google sign-in handoff refused with {Code}",
				result.Error.Code);

			return Redirect(AuthHandoff.BuildFailureRedirect(
				failedPortal, AuthHandoff.PublicErrorCode(result.Error)));
		}

		var authResponse = result.Value;

		// Which portal this returns to is decided by the account's roles, the same
		// rule the password-reset link uses, so no caller-supplied destination is
		// ever trusted.
		var portalUrl = AuthHandoff.ResolveBaseUrl(
			_configuration, Request, AuthHandoff.SettingForRoles(authResponse.Roles), _logger);

		// Two minutes: long enough for the browser to complete the hop, short
		// enough that an abandoned handoff is not a standing credential.
		var temporaryToken = Guid.NewGuid().ToString("N");
		_cache.Set(temporaryToken, authResponse, TimeSpan.FromMinutes(2));

		return Redirect(AuthHandoff.BuildHandoffRedirect(portalUrl, temporaryToken));
	}

	[HttpGet("exchange-token")]
	[EnableRateLimiting(RateLimitPolicies.OAuthHandoff)]
	public IActionResult ExchangeToken([FromQuery] string? temporaryToken)
	{
		// IMemoryCache requires a non-null key; an omitted parameter otherwise
		// surfaces as a 500 instead of the plain rejection this endpoint returns.
		if (string.IsNullOrWhiteSpace(temporaryToken))
		{
			return Unauthorized("Invalid or expired token.");
		}

		if (_cache.TryGetValue(temporaryToken, out AuthResponse? authResponse))
		{
			_cache.Remove(temporaryToken);
			return Ok(authResponse);
		}

		// Deliberately not ProblemDetails and deliberately the same text for
		// "unknown" and "expired": the response is polled by a browser and must
		// not become a probe for valid tokens.
		return Unauthorized("Invalid or expired token.");
	}

	[HttpPost("send-email-confirmation-link")]
	[EnableRateLimiting(RateLimitPolicies.Auth)]
	public async Task<IActionResult> SendEmailConfirmationLink([FromQuery] string email)
	{
		var result = await _authService.SendEmailConfirmationLink(email);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPost("send-reset-password-link")]
	[EnableRateLimiting(RateLimitPolicies.Auth)]
	public async Task<IActionResult> SendResetPasswordLink([FromQuery] string email)
	{
		var result = await _authService.SendResetPasswordLink(email);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPost("reset-password")]
	[EnableRateLimiting(RateLimitPolicies.Auth)]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
	{
		var result = await _authService.ResetPassword(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("confirm-email")]
	[EnableRateLimiting(RateLimitPolicies.Auth)]
	public async Task<IActionResult> ConfirmEmail(string id, string token)
	{
		if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(token))
		{
			return BadRequest("Null or empty values for either id or token");
		}
		var result = await _authService.ConfirmEmailAsync(id, token);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Redirect($"{Request.Scheme}://{Request.Host}/successfulEmailConfirmation.html");
	}
}