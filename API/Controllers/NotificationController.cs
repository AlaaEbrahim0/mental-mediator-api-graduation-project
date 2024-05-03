using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
	[HttpGet("user/{userId}")]
	public async Task<IActionResult> GetNotificationById(string userId)
	{
		var result = await _notificationService.GetNotificationByUserId(userId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

}
