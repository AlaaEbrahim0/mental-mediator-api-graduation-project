using Application.Contracts;
using Application.Dtos.DoctorRatings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace API.Controllers;

[ApiController]
[Route("api/doctors/{doctorId}/ratings")]
public class DoctorRatingsController : ControllerBase
{
	private readonly IDoctorRatingService _ratingService;

	public DoctorRatingsController(IDoctorRatingService ratingService)
	{
		_ratingService = ratingService;
	}

	[HttpGet]
	[Authorize]
	public async Task<IActionResult> GetRecent(string doctorId)
	{
		var result = await _ratingService.GetRecentAsync(doctorId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpGet("summary")]
	public async Task<IActionResult> GetSummary(string doctorId)
	{
		var result = await _ratingService.GetSummaryAsync(doctorId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpGet("mine")]
	[Authorize(Roles = "User")]
	public async Task<IActionResult> GetMine(string doctorId)
	{
		var result = await _ratingService.GetMineAsync(doctorId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPost]
	[Authorize(Roles = "User")]
	public async Task<IActionResult> Upsert(string doctorId, UpsertDoctorRatingRequest request)
	{
		var result = await _ratingService.UpsertAsync(doctorId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpDelete("mine")]
	[Authorize(Roles = "User")]
	public async Task<IActionResult> DeleteMine(string doctorId)
	{
		var result = await _ratingService.DeleteAsync(doctorId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return NoContent();
	}
}
