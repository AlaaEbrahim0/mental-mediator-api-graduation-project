using Shared;

namespace Domain.Errors;

public static class NotificationErrors
{
	public static Error NotFound(int id) => Error.NotFound(
		"Notifications.NotFound", $"Notification with id = '{id}' was not found");

	public static Error Forbidden() => Error.Forbidden(
		"Notifications.Forbidden", $"you have no access to this notification");
}
