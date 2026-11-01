using API.Configurations;
using Application.Contracts;
using Application.Dtos.AuthDtos;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
	private readonly IAuthService _authService;
	private readonly IConfiguration _configuration;
	private readonly ILogger<AuthController> _logger;

	public AuthController(
		IAuthService authService,
		IConfiguration configuration,
		ILogger<AuthController> logger)
	{
		_authService = authService;
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

	/// <summary>
	/// Confirms an address from a POST body. The link emailed to the user puts
	/// the token in the URL fragment, which browsers do not send to the server;
	/// the static page then submits it here so it never reaches access logs.
	/// </summary>
	[HttpPost("confirm-email")]
	[EnableRateLimiting(RateLimitPolicies.Auth)]
	public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
	{
		var result = await _authService.ConfirmEmailAsync(request.Id, request.Token);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
}