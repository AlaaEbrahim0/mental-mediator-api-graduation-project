using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/admins")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
	private readonly IAdminService _adminService;

	public AdminController(IAdminService adminService)
	{
		_adminService = adminService;
	}

	[HttpGet("summary")]
	public async Task<IActionResult> GetSystemSummary()
	{
		var result = await _adminService.GetSystemSummary();
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
}
