using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.CommentsDtos;

namespace API.Controllers;

[ApiController]
[Route("api/posts/{postId:int}/comments")]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }


    [HttpGet]
    public async Task<IActionResult> GetComments(int postId)
    {
        var result = await _commentService.GetCommentsByPostId(postId);
        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }
        return Ok(result.Value);
    }

    [HttpDelete("{commentId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int postId, int commentId)
    {
        var result = await _commentService.DeleteComment(postId, commentId);
        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }
        return Ok(result.Value);
    }
    [HttpGet("{commentId:int}")]
    public async Task<IActionResult> GetCommentById(int postId, int commentId)
    {
        var response = await _commentService.GetCommentById(postId, commentId);
        if (response.IsFailure)
        {
            return response.ToProblemDetails();
        }
        return Ok(response.Value);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateComment(int postId, [FromBody] CreateCommentRequest createCommentRequest)
    {
        var result = await _commentService.CreateComment(postId, createCommentRequest);
        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }
        return CreatedAtAction(
            nameof(GetCommentById),
            new { postId = postId, commentId = result.Value.Id },
            result.Value);

    }

    [HttpPut("{commentId:int}")]
    public async Task<IActionResult> UpdateComment(int postId, int commentId, [FromBody] UpdateCommentRequest updateCommentRequest)
    {
        var result = await _commentService.UpdateComment(postId, commentId, updateCommentRequest);
        if (result.IsFailure)
        {
            return result.ToProblemDetails();
        }
        return Ok(result.Value);
    }




}


