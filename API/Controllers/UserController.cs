using Application.Contracts;
using Application.Dtos;
using Application.Dtos.UserDtos;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
	private readonly IUserService _userService;
	private readonly MachineLearningService _machineLearningService;
	private readonly INotificationService _notificationService;

	public UserController(IUserService userService, INotificationService notificationService, MachineLearningService machineLearingService)
	{
		_userService = userService;
		_notificationService = notificationService;
		_machineLearningService = machineLearingService;
	}

	[HttpGet]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> GetAll([FromQuery] UserRequestParameters parameters)
	{
		var result = await _userService.GetAll(parameters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("{id}")]
	[Authorize(Roles = "User, Admin")]
	public async Task<IActionResult> GetUserProfile(string id)
	{
		var result = await _userService.GetUserInfo(id);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("me")]
	public async Task<IActionResult> GetUserProfile()
	{
		var result = await _userService.GetCurrentUserInfo();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
	[HttpPost("test-depression")]
	[AllowAnonymous]
	public async Task<IActionResult> TestDepression(DepressionTestRequest request)
	{
		var result = await _machineLearningService.IsDepressed(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("{id}")]
	[Authorize(Roles = "User, Admin")]
	public async Task<IActionResult> UpdateUserProfile(string id, [FromForm] UpdateUserInfoRequest request)
	{
		var result = await _userService.UpdateUserInfo(id, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("me")]
	[Authorize(Roles = "User")]
	public async Task<IActionResult> UpdateUserProfile([FromForm] UpdateUserInfoRequest request)
	{
		var result = await _userService.UpdateCurrentUserInfo(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpDelete]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> DeleteUser(string userId)
	{
		var result = await _userService.DeleteUser(userId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}


}
