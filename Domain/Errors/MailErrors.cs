using Shared;

namespace Domain.Errors;

public static class MailErrors
{
	public static Error NotConfigured() => Error.ServiceUnavailable(
		"Mail.NotConfigured",
		"Email delivery is not configured on this server, so the email could not be sent");

	public static Error DeliveryFailed(string recipient) => Error.ServiceUnavailable(
		"Mail.DeliveryFailed",
		$"The email to [{recipient}] could not be delivered");
}
