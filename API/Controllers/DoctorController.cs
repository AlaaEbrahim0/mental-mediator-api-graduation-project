using Application.Contracts;
using Application.Dtos.UserDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorController : ControllerBase
{
	private readonly IDoctorService _doctorService;
	private readonly IRepositoryManager _repos;

	public DoctorController(IDoctorService userService, IRepositoryManager repos)
	{
		_doctorService = userService;
		_repos = repos;
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

	[HttpGet("{doctorId}/schedules/{scheduleId:int}")]
	public async Task<IActionResult> GetDoctorSchedule(string doctorId, int scheduleId)
	{
		var item = await _repos.WeeklySchedules.GetById(doctorId, scheduleId, false);


		return Ok(item);

	}

}
