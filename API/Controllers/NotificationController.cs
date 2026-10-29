using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.RequestParameters;

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

	// Both portals still call GET /api/notifications/users/me, so that path stays
	// routed to the same action as the canonical one. Neither deployment order
	// can strand the other: the API answers both shapes, the portals keep working
	// unchanged, and the alias can go once both portals ship.
	[HttpGet]
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

	[HttpGet("unread-count")]
	public async Task<IActionResult> GetUnreadCount()
	{
		var result = await _notificationService.GetUnreadCountAsync();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(new { count = result.Value });
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

	[HttpPut("read-many")]
	public async Task<IActionResult> MarkMultipleAsRead([FromBody] IEnumerable<int> ids)
	{
		var result = await _notificationService.MarkMultipleAsReadAsync(ids);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
}
