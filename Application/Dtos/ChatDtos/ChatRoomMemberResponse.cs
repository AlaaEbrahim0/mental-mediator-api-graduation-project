using Domain.Enums;

namespace Application.Dtos.ChatDtos;

public class ChatRoomMemberResponse
{
	public int MemberId { get; set; }

	public string UserId { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public bool IsDoctor { get; set; }

	public ChatRoomRole Role { get; set; }

	public DateTime JoinedAtUtc { get; set; }
}
