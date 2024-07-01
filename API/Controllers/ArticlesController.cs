using Application.Contracts;
using Application.Dtos.NewsDtos;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
	private readonly INewsService _newsService;

	public ArticlesController(INewsService newsService)
	{
		_newsService = newsService;
	}

	[HttpGet]
	public async Task<IActionResult> GetArticles([FromQuery] NewsRequestParameters parameters)
	{
		var result = await _newsService.GetNews(parameters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}
}