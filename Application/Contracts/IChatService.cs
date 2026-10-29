using Application.Dtos.ChatDtos;
using Microsoft.AspNetCore.Http;
using Shared;

namespace Application.Contracts;

public interface IChatService
{
	Task<Result<ChatRoomPageResponse>> GetRoomsAsync(ChatRoomRequestParameters parameters);

	Task<Result<ChatRoomBrowseResponse>> BrowseRoomsAsync(RoomBrowseParameters parameters);

	Task<Result<ChatRoomResponse>> GetRoomAsync(int roomId);

	/// <summary>
	/// Returns the current room snapshot only when the current account is an
	/// active member. Used by the real-time hub before and after adding a
	/// connection to a room group so a concurrent leave cannot leave the
	/// connection subscribed.
	/// </summary>
	Task<Result<ChatRoomResponse>> EnsureActiveMemberAsync(int roomId);

	Task<Result<ChatRoomResponse>> CreateRoomAsync(CreateChatRoomRequest request);

	/// <summary>
	/// Joins (or idempotently re-activates membership in) a room. The passphrase is
	/// only consulted for a room that requires one and only for a caller who is not
	/// already a member.
	/// </summary>
	Task<Result<ChatRoomMemberResponse>> JoinRoomAsync(int roomId, JoinChatRoomRequest? request);

	Task<Result> LeaveRoomAsync(int roomId);

	Task<Result<IReadOnlyList<ChatRoomMemberResponse>>> GetRoomMembersAsync(int roomId);

	Task<Result<ChatMessagePageResponse>> GetMessagesAsync(int roomId, ChatMessageRequestParameters parameters);

	Task<Result<ChatMessageResponse>> SendMessageAsync(int roomId, SendChatMessageRequest request);

	Task<Result<ChatMessageResponse>> DeleteMessageAsync(long messageId);

	Task<Result<ChatMessageReportResponse>> ReportMessageAsync(long messageId, ReportChatMessageRequest request);

	Task<Result<IReadOnlyList<ChatMessageReportResponse>>> GetRoomReportsAsync(int roomId, ChatReportRequestParameters parameters);

	Task<Result<ChatMessageReportResponse>> UpdateReportStatusAsync(int roomId, int reportId, UpdateChatMessageReportStatusRequest request);

	Task<Result<ChatRoomMemberResponse>> UpdateMemberRoleAsync(int roomId, int memberId, UpdateChatRoomMemberRoleRequest request);

	Task<Result<ChatRoomResponse>> ArchiveRoomAsync(int roomId);

	Task<Result<ChatRoomResponse>> LockRoomAsync(int roomId);

	Task<Result<ChatRoomResponse>> UnlockRoomAsync(int roomId);

	Task<Result<ChatRoomResponse>> UpdateRoomAsync(int roomId, UpdateChatRoomRequest request);

	Task<Result<ChatRoomResponse>> UpdatePrivacyAsync(int roomId, UpdateRoomPrivacyRequest request);

	Task<Result<ChatRoomResponse>> SetPassphraseAsync(int roomId, SetRoomPassphraseRequest request);

	Task<Result> ClearPassphraseAsync(int roomId);

	Task<Result<ChatRoomResponse>> UpdateScheduleAsync(int roomId, UpdateRoomScheduleRequest request);

	Task<Result<ChatRoomResponse>> UploadCoverAsync(int roomId, IFormFile? image);

	Task<Result<ChatRoomResponse>> RemoveCoverAsync(int roomId);
}
