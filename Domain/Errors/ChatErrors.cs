using Shared;

namespace Domain.Errors;

public static class ChatErrors
{
	public static Error UserNotAvailable() => Error.Forbidden(
		"Chat.UserNotAvailable", "The current account is not available.");

	public static Error CreateRoomForbidden() => Error.Forbidden(
		"Chat.CreateRoomForbidden", "This account cannot create chat rooms.");

	public static Error InvalidRoomName() => Error.Validation(
		"Chat.InvalidRoomName", "Room name must be between 1 and 100 characters and cannot contain control characters.");

	public static Error InvalidRoomDescription() => Error.Validation(
		"Chat.InvalidRoomDescription", "Room description cannot exceed 500 characters or contain unsupported control characters.");

	public static Error RoomNotFound() => Error.NotFound(
		"Chat.RoomNotFound", "Chat room was not found.");

	public static Error RoomArchived() => Error.Conflict(
		"Chat.RoomArchived", "This chat room has been archived and no longer accepts new activity.");

	/// <summary>
	/// A reversible pause, unlike <see cref="RoomArchived"/>: members can still
	/// read the conversation, nobody can post and new members cannot join.
	/// </summary>
	public static Error RoomLocked() => Error.Conflict(
		"Chat.RoomLocked", "This room is paused and is not accepting messages right now.");

	public static Error RoomPassphraseRequired() => Error.Failure(
		"Chat.RoomPassphraseRequired", "This room needs a passphrase to join.");

	/// <summary>
	/// Returned for a wrong passphrase and for a throttled caller alike, so a
	/// failed join cannot be used to tell "wrong guess" from "guess too often".
	/// </summary>
	public static Error RoomPassphraseIncorrect() => Error.Failure(
		"Chat.RoomPassphraseIncorrect", "The passphrase for this room is not correct.");

	public static Error InvalidRoomPassphrase() => Error.Validation(
		"Chat.InvalidRoomPassphrase", "A room passphrase must be between 8 and 128 characters and cannot contain control characters.");

	public static Error InvalidScheduledStart() => Error.Validation(
		"Chat.InvalidScheduledStart", "A scheduled start must be in the future and no more than 90 days ahead.");

	public static Error InvalidRoomCover() => Error.Validation(
		"Chat.InvalidRoomCover", "Select an image file to use as the room cover.");

	public static Error InvalidMessagePagination() => Error.Validation(
		"Chat.InvalidMessagePagination", "Use either beforeId or afterId, not both.");

	public static Error InvalidMessageContent() => Error.Validation(
		"Chat.InvalidMessageContent", "Message content must be between 1 and 2000 characters.");

	public static Error InvalidClientMessageId() => Error.Validation(
		"Chat.InvalidClientMessageId", "clientMessageId must be a non-empty GUID.");

	public static Error MessageNotFound() => Error.NotFound(
		"Chat.MessageNotFound", "Chat message was not found.");

	public static Error MessageForbidden() => Error.Forbidden(
		"Chat.MessageForbidden", "You do not have permission to remove this message.");

	public static Error MessageDeleted() => Error.Validation(
		"Chat.MessageDeleted", "Deleted messages cannot be reported.");

	public static Error ClientMessageIdConflict() => Error.Conflict(
		"Chat.ClientMessageIdConflict", "clientMessageId has already been used for a different message.");

	public static Error ContentRejected() => Error.Validation(
		"Chat.ContentRejected", "The message violates the community content policy.");

	public static Error ModerationUnavailable() => Error.ServiceUnavailable(
		"Chat.ModerationUnavailable", "Message moderation is temporarily unavailable. Please try again.");

	public static Error CannotReportOwnMessage() => Error.Validation(
		"Chat.CannotReportOwnMessage", "You cannot report your own message.");

	public static Error ReportAlreadyExists() => Error.Conflict(
		"Chat.ReportAlreadyExists", "You have already reported this message.");

	public static Error InvalidReportReason() => Error.Validation(
		"Chat.InvalidReportReason", "Report reason must be between 1 and 1000 characters.");

	public static Error ReportNotFound() => Error.NotFound(
		"Chat.ReportNotFound", "Chat report was not found.");

	public static Error ModeratorRequired() => Error.Forbidden(
		"Chat.ModeratorRequired", "Only the room owner or a doctor moderator can perform this action.");

	public static Error OwnerRequired() => Error.Forbidden(
		"Chat.OwnerRequired", "Only the room owner can perform this action.");

	public static Error MemberNotFound() => Error.NotFound(
		"Chat.MemberNotFound", "Chat room member was not found.");

	public static Error CannotChangeOwnerRole() => Error.Conflict(
		"Chat.CannotChangeOwnerRole", "The room owner's role cannot be changed.");

	public static Error ModeratorMustBeDoctor() => Error.Validation(
		"Chat.ModeratorMustBeDoctor", "Only doctor members can be assigned the Moderator role.");

	public static Error InvalidMemberRole() => Error.Validation(
		"Chat.InvalidMemberRole", "Room members can only be assigned the Member or Moderator role.");

	public static Error InvalidReportStatus() => Error.Validation(
		"Chat.InvalidReportStatus", "A report can only be marked as Reviewed, Dismissed or Actioned.");

	public static Error OwnerCannotLeave() => Error.Conflict(
		"Chat.OwnerCannotLeave", "The room owner cannot leave the room. Archive the room instead.");
}
