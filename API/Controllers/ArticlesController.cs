using Application.Contracts;
using Application.Dtos.NewsDtos;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
	private readonly INewsService _newsService;
	public ArticlesController(INewsService newsService, IRepositoryManager repos)
	{
		_newsService = newsService;
	}

	[HttpGet]
	public async Task<IActionResult> GetArticles([FromQuery] NewsRequestParameters parameters)
	{
		var refererUrl = Request.Headers["Referer"].ToString();
		var clientIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
		Console.WriteLine(refererUrl);
		Console.WriteLine(clientIpAddress);


		var result = await _newsService.GetNews(parameters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

}