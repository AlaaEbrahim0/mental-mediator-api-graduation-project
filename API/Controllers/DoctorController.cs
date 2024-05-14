using Application.Contracts;
using Application.Dtos.UserDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize(Roles = "Doctor")]
public class DoctorController : ControllerBase
{
	private readonly IDoctorService _doctorService;

	public DoctorController(IDoctorService userService)
	{
		_doctorService = userService;
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

}
