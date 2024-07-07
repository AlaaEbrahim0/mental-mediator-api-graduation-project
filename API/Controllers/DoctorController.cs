
using Application.Contracts;
using Application.Dtos.UserDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.RequestParameters;
namespace API.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorController : ControllerBase
{
	private readonly IDoctorService _doctorService;
	private readonly INotificationService _notificationService;

	public DoctorController(IDoctorService userService, INotificationService notificationService)
	{
		_doctorService = userService;
		_notificationService = notificationService;
	}

	[HttpGet]
	[Authorize]
	public async Task<IActionResult> GetDoctors([FromQuery] DoctorRequestParameters request)
	{
		var result = await _doctorService.GetAll(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("{id}/slots")]
	[Authorize]
	public async Task<IActionResult> GetAvailableSlots(string id, [FromQuery] DateTime dateTime)
	{
		var result = await _doctorService.GetAvailableSlots(id, dateTime);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("{id}")]
	[Authorize]
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
	[Authorize(Roles = "Admin, Doctor")]
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
	[Authorize(Roles = "Doctor")]
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
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> UpdateDoctorProfile([FromForm] UpdateDoctorInfoRequest request)
	{
		var result = await _doctorService.UpdateCurrentDoctorInfo(request);
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
		var result = await _doctorService.DeleteDoctor(userId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("me/report")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetReport()
	{
		var result = await _doctorService.GetReports();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}



}
