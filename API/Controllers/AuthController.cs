using Application.Services;
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
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IMailService _mailService;
    private readonly UserManager<AppUser> _userManager;

    public AuthController(IAuthService authService, SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IMailService mailService)
    {
        _authService = authService;
        _signInManager = signInManager;
        _userManager = userManager;
        _mailService = mailService;
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

    [HttpPost("mail")]
    public async Task<IActionResult> SendMail(MailRequest mailRequest)
    {
        await _mailService.SendEmailAsync(mailRequest);
        return Ok();
    }



}
