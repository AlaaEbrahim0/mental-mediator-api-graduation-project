using API.Configurations;
using Application.Contracts;
using Application.Dtos.ChatDtos;
using Application.Dtos.ReactionsDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[Route("api/chat/messages")]
[Authorize]
public class ChatMessagesController : ControllerBase
{
	private readonly IChatService _chatService;

	public ChatMessagesController(IChatService chatService)
	{
		_chatService = chatService;
	}

	[HttpDelete("{messageId:long}")]
	public async Task<IActionResult> DeleteMessage(long messageId)
	{
		var result = await _chatService.DeleteMessageAsync(messageId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPut("{messageId:long}/reactions")]
	public async Task<IActionResult> ReactToMessage(long messageId, [FromBody] SetReactionRequest request)
	{
		var result = await _chatService.SetReactionAsync(messageId, request.Reaction);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpDelete("{messageId:long}/reactions")]
	public async Task<IActionResult> RemoveMessageReaction(long messageId)
	{
		var result = await _chatService.RemoveReactionAsync(messageId);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPost("{messageId:long}/reports")]
	[EnableRateLimiting(RateLimitPolicies.ChatReports)]
	public async Task<IActionResult> ReportMessage(
		long messageId,
		[FromBody] ReportChatMessageRequest request)
	{
		var result = await _chatService.ReportMessageAsync(messageId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}
}
