using Application.Contracts;
using Application.Dtos.SchedulingDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// The doctor's two scheduling inputs: the weekly availability periods (with
/// the consultation duration and time zone that interpret them) and time off.
/// Slots are derived from these on <c>GET /api/doctors/{id}/slots</c>.
/// </summary>
[ApiController]
[Route("api/doctors/{doctorId}")]
public class DoctorScheduleController : ControllerBase
{
	private readonly IDoctorSchedulingService _schedulingService;

	public DoctorScheduleController(IDoctorSchedulingService schedulingService)
	{
		_schedulingService = schedulingService;
	}

	[HttpGet("schedule")]
	[Authorize]
	public async Task<IActionResult> GetSchedule(string doctorId)
	{
		var result = await _schedulingService.GetScheduleAsync(doctorId);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPut("schedule")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> UpdateSchedule(string doctorId, [FromBody] UpdateDoctorScheduleRequest request)
	{
		var result = await _schedulingService.UpdateScheduleAsync(doctorId, request);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpGet("time-off")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetTimeOff(
		string doctorId,
		[FromQuery] DateOnly? from,
		[FromQuery] DateOnly? to)
	{
		var result = await _schedulingService.GetTimeOffAsync(doctorId, from, to);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPost("time-off")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> CreateTimeOff(string doctorId, [FromBody] CreateTimeOffRequest request)
	{
		var result = await _schedulingService.CreateTimeOffAsync(doctorId, request);
		return result.IsFailure
			? result.ToProblemDetails()
			: CreatedAtAction(nameof(GetTimeOff), new { doctorId }, result.Value);
	}

	[HttpDelete("time-off/{id:int}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> DeleteTimeOff(string doctorId, int id)
	{
		var result = await _schedulingService.DeleteTimeOffAsync(doctorId, id);
		return result.IsFailure ? result.ToProblemDetails() : NoContent();
	}
}
