using Application.Contracts;
using Application.Dtos.WeeklyScheduleDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers;

[ApiController]
[Route("api/doctors/{doctorId}/schedules/{scheduleId:int}")]
[Authorize(Roles = "Doctor")]
public class WeeklyScheduleController : ControllerBase
{
	private readonly IWeeklyScheduleService _scheduleService;

	public WeeklyScheduleController(IWeeklyScheduleService scheduleService)
	{
		_scheduleService = scheduleService;
	}

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetDoctorSchedule(string doctorId, int scheduleId)
	{
		var result = await _scheduleService.GetWeeklySchedule(doctorId, scheduleId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("days/{dayId:int}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetScheduleWeekDay(string doctorId, int scheduleId,
		int dayId)
	{
		var result = await _scheduleService.GetDay(doctorId, scheduleId, dayId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPost()]
	public async Task<IActionResult> CreateDoctorSchedule(string doctorId,
		CreateDoctorWeeklyScheduleRequest request)
	{
		var result = await _scheduleService.CreateWeeklySchedule(doctorId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPost("days")]
	public async Task<IActionResult> AddScheduleWeekDay(string doctorId, int scheduleId,
		CreateAvailableDayRequest request)
	{
		var result = await _scheduleService.AddDay(doctorId, scheduleId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("days/{dayId:int}")]
	public async Task<IActionResult> UpdateScheduleWeekDay(string doctorId, int scheduleId,
		int dayId, UpdateAvailableDayRequest request)
	{
		var result = await _scheduleService.UpdateDay(doctorId, scheduleId, dayId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpDelete("days/{dayId:int}")]
	public async Task<IActionResult> DeleteScheduleWeekDay(string doctorId, int scheduleId,
		int dayId)
	{
		var result = await _scheduleService.DeleteDay(doctorId, scheduleId, dayId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpDelete]
	public async Task<IActionResult> DeleteDoctorSchedule(string doctorId, int scheduleId)
	{
		var result = await _scheduleService.DeleteWeeklySchedule(doctorId, scheduleId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}



}
