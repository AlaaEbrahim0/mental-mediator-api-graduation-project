
using Application.Contracts;
using Application.Dtos.UserDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
namespace API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize(Roles = "Doctor")]
public class DoctorController : ControllerBase
{
	private readonly IDoctorService _doctorService;
	private readonly INotificationService _notificationService;

	public DoctorController(IDoctorService userService, INotificationService notificationService)
	{
		_doctorService = userService;
		_notificationService = notificationService;
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> GetDoctorProfile(string id)
	{
		var result = await _doctorService.GetDoctorInfo(id);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> UpdateDoctorProfile(string id, [FromForm] UpdateDoctorInfoRequest request)
	{
		var result = await _doctorService.UpdateDoctorInfo(id, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("me")]
	public async Task<IActionResult> GetDoctorProfile()
	{
		var result = await _doctorService.GetCurrentDoctorInfo();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("me")]
	public async Task<IActionResult> UpdateDoctorProfile([FromForm] UpdateDoctorInfoRequest request)
	{
		var result = await _doctorService.UpdateCurrentDoctorInfo(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("me/notifications")]
	public async Task<IActionResult> GetCurrentDoctorNotifications(RequestParameters paramters)
	{
		var result = await _notificationService.GetCurrentUserNotifications(paramters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}


}
