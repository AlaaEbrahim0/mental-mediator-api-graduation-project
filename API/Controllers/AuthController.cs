using Application.Services;
using Domain.Entities;
using Infrastructure;
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
    public async Task<IActionResult> Register([FromBody] RegistrationModel model)
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
        if (!result.IsAuthenticated)
        {
            return BadRequest(result.Message);
        }
        return StatusCode(201, result);
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInModel model)
    {
        if (model is null)
        {
            return UnprocessableEntity(ModelState);
        }
        var result = await _authService.SignInAsync(model);
        if (!result.IsAuthenticated)
        {
            return BadRequest(result.Message);
        }
        return Ok(result);
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
        var signInResult = await _authService.ExternalLoginAsync();
        if (!signInResult.IsAuthenticated)
        {
            return BadRequest(signInResult.Message);
        }

        return Ok(signInResult);
    }

    [HttpGet("test")]
    [Authorize]
    public IActionResult test()
    {       
        return Ok("You are authorized now");
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string id, string token)
    {
        var result = await _authService.ConfirmEmailAsync(id, token);
        if (!result.IsSuccessful)
        {
            return BadRequest(result.Message);
        }
        return Ok(result.Message);
    }


}
