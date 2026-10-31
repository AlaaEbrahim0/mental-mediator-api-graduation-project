using Application.Contracts;
using Application.Utilities;
using FluentAssertions;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Email templates are security-sensitive: they carry confirmation and reset
/// links. These tests pin HTML encoding, the single shared layout, and the
/// presence of a plain-text alternative.
/// </summary>
public class MailTemplateTests
{
	private sealed class RealWebRootProvider : IWebRootFileProvider
	{
		private readonly string _root;

		public RealWebRootProvider(string root) => _root = root;

		public string ReadFromWebRoot(string relativePath) =>
			File.ReadAllText(Path.Combine(_root, relativePath));
	}

	private static MailTemplates CreateTemplates()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);

		while (directory is not null)
		{
			var candidate = Path.Combine(directory.FullName, "API", "wwwroot");
			if (Directory.Exists(candidate))
			{
				return new MailTemplates(new RealWebRootProvider(candidate));
			}

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate API/wwwroot for template tests.");
	}

	[Fact]
	public void Confirmation_email_encodes_dynamic_values_and_is_valid_as_text()
	{
		var templates = CreateTemplates();
		var username = "<script>alert(\"x\")</script> & Co";
		var link = "https://nexus-api.example.com/confirm-email.html#id=a&token=b%2Bc";

		var email = templates.EmailConfirmation(username, link);

		email.Html.Should().NotContain("<script>alert");
		email.Html.Should().Contain("&lt;script&gt;alert");
		email.Html.Should().Contain("&amp; Co");
		email.Html.Should().Contain("token=b%2Bc");
		email.Html.Should().Contain("Nexus");
		email.Html.Should().Contain("Mental Mediator");
		email.Html.Split("<html", StringSplitOptions.None).Should().HaveCount(2);

		email.Text.Should().Contain(username);
		email.Text.Should().Contain(link);
		email.Text.Should().Contain("Nexus");
		email.Subject.Should().Be("Confirm your Nexus email address");
	}

	[Fact]
	public void Reset_email_encodes_the_link_for_the_button_and_the_fallback()
	{
		var templates = CreateTemplates();
		var link = "https://patient.example.com/forgetpassword/resetpassword?email=a%2Bb%40example.com&token=abc";

		var email = templates.ResetPassword(link);

		email.Html.Should().Contain("Reset password");
		email.Html.Should().Contain("email=a%2Bb%40example.com&amp;token=abc");
		email.Text.Should().Contain(link);
		email.Text.Should().Contain("password will not change");
	}

	[Fact]
	public void Appointment_email_encodes_user_supplied_reasons_and_has_a_text_alternative()
	{
		var templates = CreateTemplates();
		const string reason = "<img src=x onerror=alert(1)> not available";

		var email = templates.AppointmentRejected(
			"A <b>Patient</b>",
			"Dana & Co",
			42,
			reason);

		email.Html.Should().NotContain("<img");
		email.Html.Should().NotContain("<b>");
		email.Html.Should().Contain("&lt;img");
		email.Html.Should().Contain("Dana &amp; Co");
		email.Html.Should().Contain("#42");

		email.Text.Should().Contain(reason);
		email.Text.Should().Contain("Appointment: #42");
		email.Subject.Should().Be("Appointment request declined");
	}
}
