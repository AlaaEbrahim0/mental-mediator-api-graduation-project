namespace Application.Dtos.ChatDtos;

public class InviteToRoomRequest
{
	/// <summary>Account ids to invite. Validated server-side against membership.</summary>
	public List<string> UserIds { get; set; } = new();
}

public class RoomInvitationResponse
{
	public int Id { get; set; }

	public int RoomId { get; set; }

	public string RoomName { get; set; } = string.Empty;

	public string InvitedUserId { get; set; } = string.Empty;

	public string? InvitedUserName { get; set; }

	public string InvitedById { get; set; } = string.Empty;

	public string? InvitedByName { get; set; }

	/// <summary>Pending, Accepted, Declined, Cancelled or Expired.</summary>
	public string Status { get; set; } = string.Empty;

	public DateTime CreatedAtUtc { get; set; }
}

public class InviteCandidateResponse
{
	public string Id { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string? PhotoUrl { get; set; }

	public bool IsDoctor { get; set; }
}
