using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
	private readonly INotificationService _notificationService;

	public NotificationController(INotificationService notificationService)
	{
		_notificationService = notificationService;
	}


	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetNotificationById(int id)
	{
		var result = await _notificationService.GetNotificationById(id);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("users/{userId}")]
	public async Task<IActionResult> GetNotificationByUserId(string userId, RequestParameters paramters)
	{
		var result = await _notificationService.GetNotificationByUserId(userId, paramters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("users/me")]
	public async Task<IActionResult> GetCurrentUserNotifications([FromQuery] RequestParameters paramters)
	{
		var result = await _notificationService.GetCurrentUserNotifications(paramters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("users/me/read")]
	public async Task<IActionResult> MarkAllAsRead()
	{
		var result = await _notificationService.MarkAllAsReadAsync();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("{id:int}/read")]
	public async Task<IActionResult> MarkAsRead(int id)
	{
		var result = await _notificationService.MarkAsReadAsync(id);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

}
