using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.UserDtos;

namespace API.Controllers;

[ApiController]
[Route("api/profiles")]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly IUserService _userService;

    public UserProfileController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserProfile(string id)
    {
        var result = await _userService.GetUserInfo(id);
        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }
        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUserProfile(string id, [FromForm] UpdateUserInfoRequest request)
    {
        var result = await _userService.UpdateUserInfo(id, request);
        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }
        return Ok(result.Value);
    }
}
