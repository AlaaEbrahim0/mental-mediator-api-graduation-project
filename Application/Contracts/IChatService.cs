using Application.Dtos.ChatDtos;
using Microsoft.AspNetCore.Http;
using Shared;

namespace Application.Contracts;

public interface IChatService
{
	Task<Result<ChatRoomPageResponse>> GetRoomsAsync(ChatRoomRequestParameters parameters);
	Task<Result<ChatRoomBrowseResponse>> BrowseRoomsAsync(RoomBrowseParameters parameters);
	Task<Result<ChatRoomResponse>> GetRoomAsync(int roomId);
	Task<Result<ChatRoomResponse>> EnsureActiveMemberAsync(int roomId);
	Task<Result<ChatRoomResponse>> CreateRoomAsync(CreateChatRoomRequest request);
	Task<Result<ChatRoomMemberResponse>> JoinRoomAsync(int roomId, JoinChatRoomRequest? request);
	Task<Result> LeaveRoomAsync(int roomId);
	Task<Result> MarkRoomReadAsync(int roomId);
	Task<Result<IReadOnlyList<InviteCandidateResponse>>> GetInviteCandidatesAsync(int roomId, string? search);
	Task<Result<IReadOnlyList<RoomInvitationResponse>>> InviteToRoomAsync(int roomId, IReadOnlyCollection<string> userIds);
	Task<Result<IReadOnlyList<RoomInvitationResponse>>> GetRoomInvitationsAsync(int roomId);
	Task<Result> CancelRoomInvitationAsync(int roomId, int invitationId);
	Task<Result<IReadOnlyList<RoomInvitationResponse>>> GetMyInvitationsAsync();
	Task<Result<RoomInvitationResponse>> RespondToInvitationAsync(int invitationId, bool accept);
	Task<Result<IReadOnlyList<ChatRoomMemberResponse>>> GetRoomMembersAsync(int roomId);
	Task<Result<ChatMessagePageResponse>> GetMessagesAsync(int roomId, ChatMessageRequestParameters parameters);
	Task<Result<ChatMessageResponse>> SendMessageAsync(int roomId, SendChatMessageRequest request);
	Task<Result<StoredMedia>> UploadMessageMediaAsync(int roomId, IFormFile? file);
	Task<Result<ChatMessageResponse>> DeleteMessageAsync(long messageId);
	Task<Result<ChatMessageResponse>> SetReactionAsync(long messageId, string reaction);
	Task<Result<ChatMessageResponse>> RemoveReactionAsync(long messageId);
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
