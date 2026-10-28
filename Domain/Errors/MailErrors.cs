using Shared;

namespace Domain.Errors;

public static class MailErrors
{
	// Deliberately free of setting names and credential hints: the Error object
	// is serialized into the ProblemDetails response, so these strings reach the
	// caller. The specific missing setting is logged server-side by MailService.
	public static Error NotConfigured() => Error.ServiceUnavailable(
		"Mail.NotConfigured",
		"Email delivery is not configured on this server, so the email could not be sent");

	public static Error DeliveryFailed(string recipient) => Error.ServiceUnavailable(
		"Mail.DeliveryFailed",
		$"The email to [{recipient}] could not be delivered");

	public static Error MessageBuildFailed(string recipient) => Error.Failure(
		"Mail.MessageBuildFailed",
		$"The email for [{recipient}] could not be constructed from the supplied content");
}
