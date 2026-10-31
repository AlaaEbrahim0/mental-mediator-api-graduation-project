using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/history")]
[Authorize]
public class PatientHistoryController : ControllerBase
{
	private readonly IPatientHistoryService _historyService;

	public PatientHistoryController(IPatientHistoryService historyService)
	{
		_historyService = historyService;
	}

	[HttpGet("me")]
	[Authorize(Roles = "User,Admin")]
	public async Task<IActionResult> GetMyHistory(
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 20)
	{
		var result = await _historyService.GetMyHistoryAsync(pageNumber, pageSize);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpGet("patients/{patientId}")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetPatientHistory(
		string patientId,
		[FromQuery] int pageNumber = 1,
		[FromQuery] int pageSize = 20)
	{
		var result = await _historyService.GetPatientHistoryAsync(patientId, pageNumber, pageSize);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}
}
