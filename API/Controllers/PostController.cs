
using Application.Dtos.PostsDto;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace API.Controllers;

[ApiController]
[Route("api/posts")]
public class PostController : ControllerBase
{
	private readonly IPostService _postService;

	public PostController(IPostService postService)
	{
		_postService = postService;
	}

	[HttpGet]
	public async Task<IActionResult> GetPosts([FromQuery] PostRequestParameters parameters)
	{
		var result = await _postService.GetPosts(parameters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}


	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetPostById(int id)
	{
		var result = await _postService.GetPostById(id);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpGet("user/{userId}")]
	[Authorize]
	public async Task<IActionResult> GetPostsByUserId(
		[FromRoute] string userId,
		[FromQuery] PostRequestParameters parameters)
	{
		var result = await _postService.GetPostsByUserId(userId, parameters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpDelete("{id:int}")]
	[Authorize]
	public async Task<IActionResult> DeletePost(int id)
	{
		var result = await _postService.DeletePost(id);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}


		return Ok(result.Value);
	}

	[HttpPost]
	[Authorize]
	public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request)
	{
		var result = await _postService.CreatePostAsync(request);

		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return CreatedAtAction(
			nameof(GetPostById),
			new { id = result.Value.Id },
			result.Value);
	}

	[HttpPut("{id:int}")]
	[Authorize]
	public async Task<IActionResult> UpdatePost(int id, [FromForm] UpdatePostRequest updatePostRequest)
	{
		var result = await _postService.UpdatePost(id, updatePostRequest);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}


}
