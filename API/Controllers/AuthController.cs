using Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.AuthDtos;

namespace API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistrationRequest model)
    {
        if (model is null)
        {
            return UnprocessableEntity(ModelState);
        }
        var result = await _authService.RegisterAsync(model, (id, token) =>
        {
            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Auth",
                new { id = id, token = token },
                Request.Scheme);

            return confirmationLink!;
        });
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

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string id, string token)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(token))
        {
            BadRequest("Null or empty values for either id or token");
        }
        var result = await _authService.ConfirmEmailAsync(id, token);
        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }
        return Ok(result.Value);
    }

}
