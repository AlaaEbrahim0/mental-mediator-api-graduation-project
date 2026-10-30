
using Application.Contracts;
using Application.Dtos.PostsDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.RequestParameters;

namespace API.Controllers;

[ApiController]
[Route("api/posts")]
public class PostController : ControllerBase
{
	private readonly IPostService _postService;
	private readonly IStorageService _storageService;

	public PostController(IPostService postService, IStorageService storageService)
	{
		_postService = postService;
		_storageService = storageService;
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

	/// <summary>
	/// Uploads one image or short video for the composer. The returned URL is
	/// then sent with the post as a media item; uploads are separated so the
	/// client can display real progress per file.
	/// </summary>
	[HttpPost("media")]
	[Authorize]
	[RequestSizeLimit(25 * 1024 * 1024)]
	public async Task<IActionResult> UploadMedia([FromForm] UploadPostMediaRequest request)
	{
		if (request.File is null)
		{
			return Result.Failure(Error.Validation("Posts.MediaRequired", "Choose a file to upload.")).ToProblemDetails();
		}

		var result = await _storageService.UploadMedia(request.File);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(new { url = result.Value.Url, type = result.Value.Type });
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

	/// <summary>Idempotently like a post. Returns the updated like count and current user's liked state.</summary>
	[HttpPut("{id:int}/like")]
	[Authorize]
	public async Task<IActionResult> LikePost(int id)
	{
		var result = await _postService.SetLike(id, true);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	/// <summary>Idempotently remove the current user's like from a post.</summary>
	[HttpDelete("{id:int}/like")]
	[Authorize]
	public async Task<IActionResult> UnlikePost(int id)
	{
		var result = await _postService.SetLike(id, false);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}


}
