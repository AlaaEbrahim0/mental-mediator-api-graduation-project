using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Claims;
using Application.Services;
using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace API.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public AuthController(IAuthService authService, SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _authService = authService;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistrationModel model)
    {
        if (model is null)
        {
            return UnprocessableEntity(ModelState);
        }
        var result = await _authService.RegisterAsync(model);
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

    [HttpGet]
    [Route("external-login")]
    public IActionResult ExternalLogin()
    {
        var provider = "Google";
        var redirectUrl = Url.Action("ExternalLoginCallBack", "Auth");
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

        return Challenge(properties, provider);
    }
    
    [HttpGet]
    [Route("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallBack()
    {
        var result = await _authService.AddExternalLoginAsync();
        if (!result.IsAuthenticated)
        {
            return BadRequest(result.Message);
        }
        return Ok(result);
    }

    [HttpGet("test")]
    [Authorize]
    public IActionResult test()
    {       
        return Ok("You are authorized now");
    }



}
