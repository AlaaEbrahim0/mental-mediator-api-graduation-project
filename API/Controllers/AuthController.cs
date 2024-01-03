using Application.Services;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
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
            var confirmationLink = Url.Action("ConfirmEmail", "Auth", new {id = id, token = token}, Request.Scheme);
            return confirmationLink!;
        });
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
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
            return BadRequest(result.Error);
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
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string id, string token)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(token))
        {
            BadRequest("Null or empty values for either id or token");
        }
        var result = await _authService.ConfirmEmailAsync(id, token);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpGet("test")]
    [Authorize]
    public IActionResult test()
    {       
        return Ok("You are authorized now");
    }

}
