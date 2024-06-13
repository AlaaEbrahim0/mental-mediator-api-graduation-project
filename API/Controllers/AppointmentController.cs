using Application.Dtos.AppointmentDtos;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace API.Controllers;
[Route("api/appointments")]
[ApiController]
[Authorize]
public class AppointmentController : ControllerBase
{
	private readonly IAppointmentService _appointmentService;

	public AppointmentController(IAppointmentService appointmentService)
	{
		_appointmentService = appointmentService;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll([FromQuery] RequestParameters request)
	{
		var result = await _appointmentService.GetAppointements(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById(int id)
	{
		var result = await _appointmentService.GetAppointment(id);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("{id:int}/cancel")]
	public async Task<IActionResult> CancelAppointment(int id, [FromBody] string cancellationReason)
	{
		var result = await _appointmentService.CancelAppointment(id, cancellationReason);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("{id:int}/confirm")]
	public async Task<IActionResult> ConfirmAppointment(int id)
	{
		var result = await _appointmentService.ConfirmAppointment(id);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("{id:int}/reject")]
	public async Task<IActionResult> RejectAppointment(int id, [FromBody] string rejectionReason)
	{
		var result = await _appointmentService.RejectAppointment(id, rejectionReason);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPost]
	public async Task<IActionResult> CreateAppointment([FromQuery] string doctorId, [FromBody] CreateAppointmentRequest request)
	{
		var result = await _appointmentService.CreateAppointment(doctorId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
	}

}
