using System.Security.Principal;
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
    public async Task<IActionResult> GetPosts()
    {
        var result = await _postService.GetPosts();
        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPostById(int id)
    {
        var result = await _postService.GetPostById(id);
        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(int id)
    {
        var result =  await _postService.DeletePost(id);
        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize]
    public IActionResult CreatePost(CreatePostRequest request)
    {
        var result = _postService.CreatePost(request);
        return CreatedAtAction(
            nameof(GetPostById),
            new { id = result.Value.Id },
            result.Value.Message);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(int id, UpdatePostRequest updatePostRequest)
    {
        var result = await _postService.UpdatePost(id, updatePostRequest);
        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }
        return Ok(result.Value);
    }

}
