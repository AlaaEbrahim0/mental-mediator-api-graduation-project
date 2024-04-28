using Application.Contracts;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.UserDtos;

namespace API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
	private readonly IUserService _userService;
	private readonly IPostService _postService;

	public UserController(IUserService userService, IPostService postService)
	{
		_userService = userService;
		_postService = postService;
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
