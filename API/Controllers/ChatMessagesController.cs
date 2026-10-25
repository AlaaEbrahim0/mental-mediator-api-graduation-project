using API.Configurations;
using Application.Contracts;
using Application.Dtos.ChatDtos;
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
