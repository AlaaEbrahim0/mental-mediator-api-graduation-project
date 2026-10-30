namespace Domain.Enums;

public enum PostVisibility
{
	Public = 0,
	DoctorsOnly = 1,

	// Visible to active members of Post.TargetChatRoomId. A post without a
	// target is rejected on write rather than silently widened to everyone.
	SpecificGroup = 2
}
