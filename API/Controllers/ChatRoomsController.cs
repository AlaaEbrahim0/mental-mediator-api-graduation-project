using API.Configurations;
using Application.Contracts;
using Application.Dtos.ChatDtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

	[HttpGet("browse")]
	public async Task<IActionResult> BrowseRooms([FromQuery] RoomBrowseParameters parameters)
	{
		var result = await _chatService.BrowseRoomsAsync(parameters);
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
	[EnableRateLimiting(RateLimitPolicies.ChatRoomJoin)]
	public async Task<IActionResult> JoinRoom(
		int roomId,
		[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JoinChatRoomRequest? request)
	{
		var result = await _chatService.JoinRoomAsync(roomId, request);
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

	[HttpPatch("{roomId:int}")]
	public async Task<IActionResult> UpdateRoom(
		int roomId,
		[FromBody] UpdateChatRoomRequest request)
	{
		var result = await _chatService.UpdateRoomAsync(roomId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPost("{roomId:int}/lock")]
	public async Task<IActionResult> LockRoom(int roomId)
	{
		var result = await _chatService.LockRoomAsync(roomId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPost("{roomId:int}/unlock")]
	public async Task<IActionResult> UnlockRoom(int roomId)
	{
		var result = await _chatService.UnlockRoomAsync(roomId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPut("{roomId:int}/privacy")]
	public async Task<IActionResult> UpdatePrivacy(
		int roomId,
		[FromBody] UpdateRoomPrivacyRequest request)
	{
		var result = await _chatService.UpdatePrivacyAsync(roomId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPut("{roomId:int}/passphrase")]
	public async Task<IActionResult> SetPassphrase(
		int roomId,
		[FromBody] SetRoomPassphraseRequest request)
	{
		var result = await _chatService.SetPassphraseAsync(roomId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpDelete("{roomId:int}/passphrase")]
	public async Task<IActionResult> ClearPassphrase(int roomId)
	{
		var result = await _chatService.ClearPassphraseAsync(roomId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return NoContent();
	}

	[HttpPut("{roomId:int}/scheduled")]
	public async Task<IActionResult> UpdateSchedule(
		int roomId,
		[FromBody] UpdateRoomScheduleRequest request)
	{
		var result = await _chatService.UpdateScheduleAsync(roomId, request);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpPut("{roomId:int}/cover")]
	[Consumes("multipart/form-data")]
	public async Task<IActionResult> UploadCover(
		int roomId,
		[FromForm] UpdateRoomCoverRequest request)
	{
		var result = await _chatService.UploadCoverAsync(roomId, request.Image);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}

	[HttpDelete("{roomId:int}/cover")]
	public async Task<IActionResult> RemoveCover(int roomId)
	{
		var result = await _chatService.RemoveCoverAsync(roomId);
		if (result.IsFailure)
		{
			return result.ToProblemDetails();
		}

		return Ok(result.Value);
	}
}
