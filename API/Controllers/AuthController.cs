using Application.Contracts;
using Application.Dtos.AuthDtos;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

	public AuthController(IAuthService authService, SignInManager<BaseUser> signInManager, IMemoryCache cache, IConfiguration configuration)
	{
		_authService = authService;
		_signInManager = signInManager;
		_cache = cache;
		_configuration = configuration;
	}

	[HttpPost("register")]
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
	public IActionResult ExternalLogin()
	{

		var provider = "Google";
		var redirectUrl = Url.Action("ExternalLoginCallback", "Auth", new { }, Request.Scheme);
		var properties = _authService.GetExternalAuthenticationProperties(provider, redirectUrl);

		return Challenge(properties, provider);
	}

	[HttpGet("external-login-callback")]
	public async Task<IActionResult> ExternalLoginCallback()
	{
		var result = await _authService.ExternalLoginAsync();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		var authResponse = result.Value;

		var temporaryToken = Guid.NewGuid().ToString();

		_cache.Set(temporaryToken, authResponse, TimeSpan.FromMinutes(10));

		var callerUrl = _configuration["ClientUI"];
		var redirectUrl = $"{callerUrl}?temporaryToken={temporaryToken}";

		return Redirect(redirectUrl);
	}

	[HttpGet("exchange-token")]
	public IActionResult ExchangeToken([FromQuery] string temporaryToken)
	{
		if (_cache.TryGetValue(temporaryToken, out AuthResponse? authResponse))
		{
			_cache.Remove(temporaryToken);
			return Ok(authResponse);
		}

		return Unauthorized("Invalid or expired token.");
	}


	[HttpPost("send-email-confirmation-link")]
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
