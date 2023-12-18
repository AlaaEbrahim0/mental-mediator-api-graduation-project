using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace API.Controllers;

[ApiController]
[Route("api")]
public class AuthenticationController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthenticationController(IAuthenticationService authService)
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

    [HttpGet("test")]
    [Authorize]
    public IActionResult test()
    {
        return Ok("You are authorized now");
    }

}
