using Application.Contracts;
using Application.Dtos.DoctorWorkspaceDtos;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// The doctor's own workspace. Distinct from <c>DoctorController</c> (which
/// serves public doctor data and <c>me/report</c>): every route here is scoped
/// to the caller and never accepts another doctor's id.
/// </summary>
[ApiController]
[Route("api/doctors/me")]
[Authorize(Roles = "Doctor")]
public class DoctorWorkspaceController : ControllerBase
{
	private readonly IDoctorWorkspaceService _workspaceService;

	public DoctorWorkspaceController(IDoctorWorkspaceService workspaceService)
	{
		_workspaceService = workspaceService;
	}

	[HttpGet("dashboard")]
	public async Task<IActionResult> GetDashboard()
	{
		var result = await _workspaceService.GetDashboardAsync();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpGet("activity")]
	public async Task<IActionResult> GetActivity([FromQuery] int limit = 20)
	{
		var result = await _workspaceService.GetActivityAsync(limit);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpGet("notes")]
	public async Task<IActionResult> GetNotes(
		[FromQuery] string? patientId,
		[FromQuery] int limit = 20)
	{
		var result = await _workspaceService.GetNotesAsync(patientId, limit);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPost("notes")]
	public async Task<IActionResult> CreateNote([FromBody] CreatePatientNoteRequest request)
	{
		var result = await _workspaceService.CreateNoteAsync(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return CreatedAtAction(nameof(GetNotes), new { patientId = result.Value.PatientId }, result.Value);
	}

	[HttpPatch("notes/{noteId:int}")]
	public async Task<IActionResult> UpdateNote(
		int noteId,
		[FromBody] UpdatePatientNoteRequest request)
	{
		var result = await _workspaceService.UpdateNoteAsync(noteId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpGet("follow-ups")]
	public async Task<IActionResult> GetFollowUps(
		[FromQuery] FollowUpStatus? status,
		[FromQuery] int limit = 20)
	{
		var result = await _workspaceService.GetFollowUpsAsync(status, limit);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPost("follow-ups")]
	public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpRequest request)
	{
		var result = await _workspaceService.CreateFollowUpAsync(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return CreatedAtAction(nameof(GetFollowUps), null, result.Value);
	}

	[HttpPatch("follow-ups/{followUpId:int}")]
	public async Task<IActionResult> UpdateFollowUp(
		int followUpId,
		[FromBody] UpdateFollowUpRequest request)
	{
		var result = await _workspaceService.UpdateFollowUpAsync(followUpId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}
}
