using API.Configurations;
using Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/chat/invitations")]
[Authorize]
public class ChatInvitationsController : ControllerBase
{
	private readonly IChatService _chatService;

	public ChatInvitationsController(IChatService chatService)
	{
		_chatService = chatService;
	}

	[HttpGet("mine")]
	public async Task<IActionResult> GetMyInvitations()
	{
		var result = await _chatService.GetMyInvitationsAsync();
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPost("{invitationId:int}/accept")]
	public async Task<IActionResult> Accept(int invitationId)
	{
		var result = await _chatService.RespondToInvitationAsync(invitationId, accept: true);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}

	[HttpPost("{invitationId:int}/decline")]
	public async Task<IActionResult> Decline(int invitationId)
	{
		var result = await _chatService.RespondToInvitationAsync(invitationId, accept: false);
		return result.IsFailure ? result.ToProblemDetails() : Ok(result.Value);
	}
}
