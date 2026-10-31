using System.Globalization;
using System.Text.Encodings.Web;
using Application.Contracts;

namespace Application.Utilities;

/// <summary>One rendered transactional email: subject, HTML and plain text.</summary>
public sealed record EmailContent(string Subject, string Html, string Text);

/// <summary>
/// Renders transactional email from a shared layout plus one small body
/// template. Every dynamic value is HTML-encoded before it reaches the
/// document; only the rendered body is inserted unencoded into the layout.
/// </summary>
public class MailTemplates
{
	private const string LayoutPath = "email-templates/layout.html";
	private const string TemplateDirectory = "email-templates";

	private static readonly HtmlEncoder Encoder = HtmlEncoder.Default;

	private readonly IWebRootFileProvider _fileProvider;

	public MailTemplates(IWebRootFileProvider fileProvider)
	{
		_fileProvider = fileProvider;
	}

	public EmailContent EmailConfirmation(string username, string confirmationLink)
	{
		var html = Render(
			"email-confirmation.html",
			"Confirm your email address",
			"Confirm your email address to finish setting up your Nexus account.",
			new Dictionary<string, string>
			{
				["username"] = username,
				["confirmationLink"] = confirmationLink,
			});

		var text =
			$"Hello {username},\n\n" +
			"Thank you for creating a Nexus account. Confirm your email address to finish setting up your account:\n\n" +
			$"{confirmationLink}\n\n" +
			"If you did not create this account, you can ignore this email.\n\n" +
			"Nexus · Mental Mediator";

		return new EmailContent("Confirm your Nexus email address", html, text);
	}

	public EmailContent ResetPassword(string resetPasswordLink)
	{
		var html = Render(
			"reset-password.html",
			"Reset your password",
			"Use the secure link to choose a new Nexus password.",
			new Dictionary<string, string>
			{
				["resetPasswordLink"] = resetPasswordLink,
			});

		var text =
			"Hello,\n\n" +
			"We received a request to reset your Nexus password. Choose a new password using this link:\n\n" +
			$"{resetPasswordLink}\n\n" +
			"If you did not request a reset, you can ignore this email; your password will not change.\n\n" +
			"Nexus · Mental Mediator";

		return new EmailContent("Reset your Nexus password", html, text);
	}

	public EmailContent AppointmentRequested(
		string doctorName,
		string clientName,
		int appointmentId,
		DateTime startTimeUtc)
	{
		return AppointmentMessage(
			"New appointment request",
			doctorName,
			$"{clientName} requested an appointment with you.",
			appointmentId,
			startTimeUtc,
			"Review the request in your Nexus appointments to confirm or decline it.");
	}

	public EmailContent AppointmentConfirmed(
		string clientName,
		string doctorName,
		int appointmentId,
		DateTime startTimeUtc)
	{
		return AppointmentMessage(
			"Appointment confirmed",
			clientName,
			$"Dr. {doctorName} confirmed your appointment.",
			appointmentId,
			startTimeUtc,
			"You can review the details in your Nexus appointments.");
	}

	public EmailContent AppointmentRejected(
		string clientName,
		string doctorName,
		int appointmentId,
		string? reason)
	{
		return AppointmentMessage(
			"Appointment request declined",
			clientName,
			$"Unfortunately, Dr. {doctorName} could not accept the appointment request.",
			appointmentId,
			startTimeUtc: null,
			note: string.IsNullOrWhiteSpace(reason)
				? "You can search for another available doctor in Nexus."
				: $"Reason provided: {reason.Trim()}");
	}

	public EmailContent AppointmentCancelled(
		string doctorName,
		string clientName,
		int appointmentId,
		string? reason)
	{
		return AppointmentMessage(
			"Appointment cancelled",
			doctorName,
			$"{clientName} cancelled the appointment.",
			appointmentId,
			startTimeUtc: null,
			note: string.IsNullOrWhiteSpace(reason)
				? "The slot is now available to other patients."
				: $"Reason provided: {reason.Trim()}");
	}

	public EmailContent AppointmentCompleted(
		string clientName,
		string doctorName,
		int appointmentId)
	{
		return AppointmentMessage(
			"Appointment completed",
			clientName,
			$"Dr. {doctorName} marked your appointment as completed.",
			appointmentId,
			startTimeUtc: null,
			note: "We hope the session was helpful. You can review your care history in Nexus.");
	}

	private EmailContent AppointmentMessage(
		string subject,
		string recipientName,
		string intro,
		int appointmentId,
		DateTime? startTimeUtc,
		string note)
	{
		var when = startTimeUtc.HasValue
			? startTimeUtc.Value.ToString(
				"dddd, d MMMM yyyy 'at' HH:mm 'UTC'",
				CultureInfo.InvariantCulture)
			: "See your Nexus appointments for the scheduled time";

		var html = Render(
			"appointment-update.html",
			subject,
			intro,
			new Dictionary<string, string>
			{
				["recipientName"] = recipientName,
				["intro"] = intro,
				["appointmentId"] = appointmentId.ToString(CultureInfo.InvariantCulture),
				["appointmentWhen"] = when,
				["note"] = note,
			});

		var text =
			$"Hello {recipientName},\n\n" +
			$"{intro}\n\n" +
			$"Appointment: #{appointmentId}\n" +
			$"When: {when}\n\n" +
			$"{note}\n\n" +
			"You can review your appointments in your Nexus account.\n\n" +
			"Nexus · Mental Mediator";

		return new EmailContent(subject, html, text);
	}

	private string Render(
		string bodyTemplate,
		string title,
		string preheader,
		IReadOnlyDictionary<string, string> values)
	{
		var layout = _fileProvider.ReadFromWebRoot(LayoutPath);
		var body = _fileProvider.ReadFromWebRoot($"{TemplateDirectory}/{bodyTemplate}");
		var renderedBody = ReplaceTokens(body, values);

		return layout
			.Replace("{{title}}", Encoder.Encode(title))
			.Replace("{{preheader}}", Encoder.Encode(preheader))
			.Replace("{{content}}", renderedBody)
			.Replace("{{year}}", DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture));
	}

	private static string ReplaceTokens(
		string template,
		IReadOnlyDictionary<string, string> values)
	{
		foreach (var (token, value) in values)
		{
			template = template.Replace($"{{{{{token}}}}}", Encoder.Encode(value));
		}

		return template;
	}
}
