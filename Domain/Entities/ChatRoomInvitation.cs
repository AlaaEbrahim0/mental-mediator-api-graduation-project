using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// A room moderator's invitation to a specific user. An invitation is the only
/// way a private room admits a newcomer without a passphrase, so acceptance —
/// not creation — is what grants membership. Invited users see nothing about
/// the room until they accept.
/// </summary>
public class ChatRoomInvitation
{
	public int Id { get; set; }

	public int RoomId { get; set; }

	public ChatRoom Room { get; set; } = null!;

	public string InvitedUserId { get; set; } = string.Empty;

	public BaseUser InvitedUser { get; set; } = null!;

	public string InvitedById { get; set; } = string.Empty;

	public BaseUser? InvitedBy { get; set; }

	public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? RespondedAtUtc { get; set; }
}
