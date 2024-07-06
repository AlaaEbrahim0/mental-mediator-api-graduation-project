using Application.Contracts;
using Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace API.Controllers;
[Route("api/depression-tests")]
[ApiController]
public class DepressionTestController : ControllerBase
{
	private readonly IDepressionTestService _depressionTestService;

	public DepressionTestController(IDepressionTestService depressionTestService)
	{
		_depressionTestService = depressionTestService;
	}

	[HttpPost]
	[AllowAnonymous]
	public async Task<IActionResult> TestDepression(DepressionTestRequest request)
	{
		var result = await _depressionTestService.CreateAndGetDepressionTestResult(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
	[HttpGet]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> GetAllTestsResults([FromQuery] DepressionTestsRequestParameters parameters)
	{
		var result = await _depressionTestService.GetAllTestResults(parameters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}


}
