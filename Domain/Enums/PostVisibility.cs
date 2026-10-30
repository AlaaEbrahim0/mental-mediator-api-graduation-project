namespace Domain.Enums;

public enum PostVisibility
{
	Public = 0,
	DoctorsOnly = 1,

	// Reserved until the application has a group model to target. The API
	// rejects it on write rather than silently publishing to everyone.
	SpecificGroup = 2
}
