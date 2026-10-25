using Application.Dtos.ChatDtos;
using Shared;

namespace Application.Contracts;

public interface IChatService
{
	Task<Result<ChatRoomPageResponse>> GetRoomsAsync(ChatRoomRequestParameters parameters);

	Task<Result<ChatRoomResponse>> GetRoomAsync(int roomId);

	/// <summary>
	/// Returns the current room snapshot only when the current account is an
	/// active member. Used by the real-time hub before and after adding a
	/// connection to a room group so a concurrent leave cannot leave the
	/// connection subscribed.
	/// </summary>
	Task<Result<ChatRoomResponse>> EnsureActiveMemberAsync(int roomId);

	Task<Result<ChatRoomResponse>> CreateRoomAsync(CreateChatRoomRequest request);

	Task<Result<ChatRoomMemberResponse>> JoinRoomAsync(int roomId);

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
}
