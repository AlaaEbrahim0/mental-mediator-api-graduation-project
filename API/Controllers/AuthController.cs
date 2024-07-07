using Application.Contracts;
using Application.Dtos.AuthDtos;
using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
	private readonly IAuthService _authService;
	private readonly SignInManager<BaseUser> _signInManager;

	public AuthController(IAuthService authService, SignInManager<BaseUser> signInManager)
	{
		_authService = authService;
		_signInManager = signInManager;
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

		return Ok(result.Value);
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
