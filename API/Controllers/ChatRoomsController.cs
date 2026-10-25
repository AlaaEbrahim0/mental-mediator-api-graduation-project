using API.Configurations;
using Application.Contracts;
using Application.Dtos.ChatDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[Route("api/chat/rooms")]
[Authorize]
public class ChatRoomsController : ControllerBase
{
	private readonly IChatService _chatService;

	public ChatRoomsController(IChatService chatService)
	{
		_chatService = chatService;
	}

	[HttpGet]
	public async Task<IActionResult> GetRooms([FromQuery] ChatRoomRequestParameters parameters)
	{
		var result = await _chatService.GetRoomsAsync(parameters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpGet("{roomId:int}")]
	public async Task<IActionResult> GetRoom(int roomId)
	{
		var result = await _chatService.GetRoomAsync(roomId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPost]
	[Authorize(Roles = "Doctor,Admin")]
	[EnableRateLimiting(RateLimitPolicies.ChatRoomCreation)]
	public async Task<IActionResult> CreateRoom([FromBody] CreateChatRoomRequest request)
	{
		var result = await _chatService.CreateRoomAsync(request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return CreatedAtAction(
			nameof(GetRoom),
			new { roomId = result.Value.Id },
			result.Value);
	}

	[HttpPost("{roomId:int}/join")]
	public async Task<IActionResult> JoinRoom(int roomId)
	{
		var result = await _chatService.JoinRoomAsync(roomId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPost("{roomId:int}/leave")]
	public async Task<IActionResult> LeaveRoom(int roomId)
	{
		var result = await _chatService.LeaveRoomAsync(roomId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return NoContent();
	}

	[HttpGet("{roomId:int}/members")]
	public async Task<IActionResult> GetMembers(int roomId)
	{
		var result = await _chatService.GetRoomMembersAsync(roomId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpGet("{roomId:int}/messages")]
	public async Task<IActionResult> GetMessages(
		int roomId,
		[FromQuery] ChatMessageRequestParameters parameters)
	{
		var result = await _chatService.GetMessagesAsync(roomId, parameters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPost("{roomId:int}/messages")]
	[EnableRateLimiting(RateLimitPolicies.ChatMessages)]
	public async Task<IActionResult> SendMessage(
		int roomId,
		[FromBody] SendChatMessageRequest request)
	{
		var result = await _chatService.SendMessageAsync(roomId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpGet("{roomId:int}/reports")]
	public async Task<IActionResult> GetReports(
		int roomId,
		[FromQuery] ChatReportRequestParameters parameters)
	{
		var result = await _chatService.GetRoomReportsAsync(roomId, parameters);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPatch("{roomId:int}/reports/{reportId:int}")]
	public async Task<IActionResult> UpdateReportStatus(
		int roomId,
		int reportId,
		[FromBody] UpdateChatMessageReportStatusRequest request)
	{
		var result = await _chatService.UpdateReportStatusAsync(roomId, reportId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPatch("{roomId:int}/members/{memberId:int}/role")]
	public async Task<IActionResult> UpdateMemberRole(
		int roomId,
		int memberId,
		[FromBody] UpdateChatRoomMemberRoleRequest request)
	{
		var result = await _chatService.UpdateMemberRoleAsync(roomId, memberId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPost("{roomId:int}/archive")]
	public async Task<IActionResult> ArchiveRoom(int roomId)
	{
		var result = await _chatService.ArchiveRoomAsync(roomId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}
}
