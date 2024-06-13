﻿using Application.Contracts;
using Application.Dtos.UserDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
	private readonly IUserService _userService;
	private readonly INotificationService _notificationService;

	public UserController(IUserService userService, INotificationService notificationService)
	{
		_userService = userService;
		_notificationService = notificationService;
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

	[HttpPut("me")]
	public async Task<IActionResult> UpdateUserProfile([FromForm] UpdateUserInfoRequest request)
	{
		var result = await _userService.UpdateCurrentUserInfo(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}


}
