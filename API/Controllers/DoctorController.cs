
using Application.Contracts;
using Application.Dtos.UserDtos;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.RequestParameters;
namespace API.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorController : ControllerBase
{
	private readonly IDoctorService _doctorService;
	private readonly INotificationService _notificationService;
	private readonly IDoctorSchedulingService _schedulingService;

	public DoctorController(IDoctorService userService, INotificationService notificationService, IDoctorSchedulingService schedulingService)
	{
		_doctorService = userService;
		_notificationService = notificationService;
		_schedulingService = schedulingService;
	}

	[HttpGet]
	[Authorize]
	public async Task<IActionResult> GetDoctors([FromQuery] DoctorRequestParameters request)
	{
		var errors = new Dictionary<string, string[]>();
		if (request.MinFees < 0 || request.MaxFees < 0)
			errors["fees"] = new[] { "Fee bounds must be zero or greater." };
		if (request.MinFees.HasValue && request.MaxFees.HasValue && request.MinFees.Value > request.MaxFees.Value)
			errors["maxFees"] = new[] { "Maximum fees must be greater than or equal to minimum fees." };
		if (!string.IsNullOrWhiteSpace(request.Specialization) &&
			(!Enum.TryParse<DoctorSpecialization>(request.Specialization, true, out var specialization) || !Enum.IsDefined(specialization)))
			errors["specialization"] = new[] { "Specialization must be a valid value." };
		if (!string.IsNullOrWhiteSpace(request.Gender) && !new[] { "male", "female" }.Contains(request.Gender, StringComparer.OrdinalIgnoreCase))
			errors["gender"] = new[] { "Gender must be male or female." };
		if (errors.Count > 0)
			return BadRequest(new ValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest, Title = "Validation Failed" });

		var result = await _doctorService.GetAll(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("{id}/slots")]
	[Authorize]
	public async Task<IActionResult> GetAvailableSlots(
		string id,
		[FromQuery] DateOnly? date,
		[FromQuery] DateOnly? from,
		[FromQuery] DateOnly? to)
	{
		DateOnly rangeStart;
		DateOnly rangeEnd;

		if (date.HasValue)
		{
			if (from.HasValue || to.HasValue)
			{
				return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
				{
					["range"] = new[] { "Use either date or from/to, not both." }
				})
				{
					Status = StatusCodes.Status400BadRequest,
					Title = "Validation Failed"
				});
			}

			rangeStart = date.Value;
			rangeEnd = date.Value;
		}
		else if (from.HasValue && to.HasValue)
		{
			rangeStart = from.Value;
			rangeEnd = to.Value;
		}
		else
		{
			return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
			{
				["range"] = new[] { "Provide date, or both from and to." }
			})
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Validation Failed"
			});
		}

		var result = await _schedulingService.GetAvailabilityAsync(id, rangeStart, rangeEnd);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("{id}")]
	[Authorize]
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
	[Authorize(Roles = "Admin, Doctor")]
	public async Task<IActionResult> UpdateDoctorProfile(string id, [FromForm] UpdateDoctorInfoRequest request)
	{
		var result = await _doctorService.UpdateDoctorInfo(id, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("me")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetDoctorProfile()
	{
		var result = await _doctorService.GetCurrentDoctorInfo();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("me")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> UpdateDoctorProfile([FromForm] UpdateDoctorInfoRequest request)
	{
		var result = await _doctorService.UpdateCurrentDoctorInfo(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpDelete]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> DeleteUser(string userId)
	{
		var result = await _doctorService.DeleteDoctor(userId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("me/report")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetReport()
	{
		var result = await _doctorService.GetReports();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}



}
