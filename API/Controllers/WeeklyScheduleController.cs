﻿using Application.Contracts;
using Application.Dtos.WeeklyScheduleDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers;

[ApiController]
[Route("api/doctors/{doctorId}/schedule/")]
public class WeeklyScheduleController : ControllerBase
{
	private readonly IWeeklyScheduleService _scheduleService;

	public WeeklyScheduleController(IWeeklyScheduleService scheduleService)
	{
		_scheduleService = scheduleService;
	}

	[HttpGet]
	[Authorize]
	public async Task<IActionResult> GetSchedule(string doctorId)
	{
		var result = await _scheduleService.GetWeeklySchedule(doctorId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}


	[HttpGet("days/{day}")]
	[Authorize]
	public async Task<IActionResult> GetScheduleDay(string doctorId, DayOfWeek day)
	{
		var result = await _scheduleService.GetDay(doctorId, day);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
	[HttpPut("days/{day}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> UpdateScheduleDay(string doctorId, DayOfWeek day, UpdateScheduleWeekDayRequest request)
	{
		var result = await _scheduleService.UpdateDay(doctorId, day, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
	[HttpPost("days")]
	[Authorize(Roles = "Doctor")]

	public async Task<IActionResult> AddScheduleDay(string doctorId, CreateScheduleWeekDayRequest request)
	{
		var result = await _scheduleService.AddDay(doctorId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
	[HttpDelete("days/{day}")]
	[Authorize(Roles = "Doctor")]

	public async Task<IActionResult> DeleteScheduleDay(string doctorId, DayOfWeek day)
	{
		var result = await _scheduleService.DeleteDay(doctorId, day);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPost]
	[Authorize(Roles = "Doctor")]

	public async Task<IActionResult> CreateSchedule(string doctorId,
		CreateDoctorWeeklyScheduleRequest request)
	{
		var result = await _scheduleService.CreateWeeklySchedule(doctorId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpDelete]
	[Authorize(Roles = "Doctor")]

	public async Task<IActionResult> DeleteSchedule(string doctorId)
	{
		var result = await _scheduleService.DeleteWeeklySchedule(doctorId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

}
