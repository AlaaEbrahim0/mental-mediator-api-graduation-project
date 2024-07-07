using Application.Contracts;
using Application.Dtos.ReplyDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/posts/{postId:int}/comments/{commentId:int}/replies/")]
[Authorize]
public class ReplyController : ControllerBase
{
	private readonly IReplyService _replyService;

	public ReplyController(IReplyService replyService)
	{
		_replyService = replyService;
	}

	[HttpGet]
	[AllowAnonymous]
	public async Task<IActionResult> GetRepliesForComment(int postId, int commentId)
	{
		var result = await _replyService.GetRepliesForComment(postId, commentId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPost]
	public async Task<IActionResult> AddReplyToComment(int postId, int commentId, CreateReplyRequest request)
	{
		var result = await _replyService.CreateReply(postId, commentId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return CreatedAtAction(
			nameof(GetReplyById),
			new { postId = postId, commentId = commentId, replyId = result.Value.Id },
			result.Value);
	}

	[HttpGet("{replyId:int}")]
	[AllowAnonymous]
	public async Task<IActionResult> GetReplyById(int postId, int commentId, int replyId)
	{
		var result = await _replyService.GetReplyById(postId, commentId, replyId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpPut("{replyId:int}")]
	public async Task<IActionResult> UpdateReply(int postId, int commentId, int replyId, UpdateReplyRequest request)
	{
		var result = await _replyService.UpdateReply(postId, commentId, replyId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

	[HttpDelete("{replyId:int}")]
	public async Task<IActionResult> DeleteReply(int postId, int commentId, int replyId)
	{
		var result = await _replyService.DeleteReply(postId, commentId, replyId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}
		return Ok(result.Value);
	}

}
