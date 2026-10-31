using FluentValidation;

namespace Application.Dtos.NotificationDtos;
public class MailRequest
{
	public string? ToEmail { get; set; }

	public string? Subject { get; set; }

	public string? Body { get; set; }

	/// <summary>
	/// Optional plain-text alternative. Clients that cannot or do not render
	/// HTML receive this version; it never contains a different message.
	/// </summary>
	public string? TextBody { get; set; }
}

public class MailRequestValidator : AbstractValidator<MailRequest>
{
	public MailRequestValidator()
	{
		RuleFor(x => x.ToEmail)
			.NotEmpty()
			.EmailAddress()
			.WithMessage("Invalid email address");
	}
}