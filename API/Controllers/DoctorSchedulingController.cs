using Application.Contracts;
using Application.Dtos.SchedulingDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Doctor scheduling primitives: recurring breaks, date-specific availability
/// overrides, one-off UTC time blocks and per-doctor scheduling settings.
///
/// These endpoints describe when a doctor is *not* bookable. The slot
/// calculation that consumes them is exposed on
/// <c>GET /api/doctors/{doctorId}/slots</c>.
/// </summary>
[ApiController]
[Route("api/doctors/{doctorId}/scheduling")]
[Authorize]
public class DoctorSchedulingController : ControllerBase
{
	private readonly IDoctorSchedulingService _schedulingService;

	public DoctorSchedulingController(IDoctorSchedulingService schedulingService)
	{
		_schedulingService = schedulingService;
	}

	[HttpGet("breaks")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetRecurringBreaks(string doctorId)
	{
		var result = await _schedulingService.GetRecurringBreaksAsync(doctorId);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPost("breaks")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> CreateRecurringBreak(string doctorId, [FromBody] CreateRecurringBreakRequest request)
	{
		var result = await _schedulingService.CreateRecurringBreakAsync(doctorId, request);
		return result.IsFailure
			? result.ToProblemDetails()
			: CreatedAtAction(nameof(GetRecurringBreaks), new { doctorId }, result.Value);
	}

	[HttpDelete("breaks/{id:int}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> DeleteRecurringBreak(string doctorId, int id)
	{
		var result = await _schedulingService.DeleteRecurringBreakAsync(doctorId, id);
		return result.IsFailure ? result.ToProblemDetails() : NoContent();
	}

	[HttpGet("overrides")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetOverrides(string doctorId, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
	{
		var result = await _schedulingService.GetOverridesAsync(doctorId, from, to);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPost("overrides")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> CreateOverride(string doctorId, [FromBody] CreateAvailabilityOverrideRequest request)
	{
		var result = await _schedulingService.CreateOverrideAsync(doctorId, request);
		return result.IsFailure
			? result.ToProblemDetails()
			: CreatedAtAction(nameof(GetOverrides), new { doctorId }, result.Value);
	}

	[HttpDelete("overrides/{id:int}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> DeleteOverride(string doctorId, int id)
	{
		var result = await _schedulingService.DeleteOverrideAsync(doctorId, id);
		return result.IsFailure ? result.ToProblemDetails() : NoContent();
	}

	[HttpGet("blocks")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetTimeBlocks(string doctorId, [FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc)
	{
		var result = await _schedulingService.GetTimeBlocksAsync(doctorId, fromUtc, toUtc);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPost("blocks")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> CreateTimeBlock(string doctorId, [FromBody] CreateTimeBlockRequest request)
	{
		var result = await _schedulingService.CreateTimeBlockAsync(doctorId, request);
		return result.IsFailure
			? result.ToProblemDetails()
			: CreatedAtAction(nameof(GetTimeBlocks), new { doctorId }, result.Value);
	}

	[HttpPut("blocks/{id:int}/cancel")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> CancelTimeBlock(string doctorId, int id)
	{
		var result = await _schedulingService.CancelTimeBlockAsync(doctorId, id);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpGet("settings")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetSettings(string doctorId)
	{
		var result = await _schedulingService.GetSettingsAsync(doctorId);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPut("settings")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> UpdateSettings(string doctorId, [FromBody] UpdateSchedulingSettingsRequest request)
	{
		var result = await _schedulingService.UpdateSettingsAsync(doctorId, request);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}
}
