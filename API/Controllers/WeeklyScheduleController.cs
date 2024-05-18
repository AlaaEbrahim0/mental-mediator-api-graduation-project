using Application.Contracts;
using Application.Dtos.WeeklyScheduleDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers;

[ApiController]
[Route("api/doctors/{doctorId}/schedule/")]
[Authorize(Roles = "Doctor")]
public class WeeklyScheduleController : ControllerBase
{
	private readonly IWeeklyScheduleService _scheduleService;

	public WeeklyScheduleController(IWeeklyScheduleService scheduleService)
	{
		_scheduleService = scheduleService;
	}

	[HttpGet]
	public async Task<IActionResult> GetSchedule(string doctorId)
	{
		var result = await _scheduleService.GetWeeklySchedule(doctorId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
	[HttpPost]
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



}
