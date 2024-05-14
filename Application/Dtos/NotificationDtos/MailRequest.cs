using FluentValidation;

namespace Application.Dtos.NotificationDtos;
public class MailRequest
{
	public string? ToEmail { get; set; }

	public string? Subject { get; set; }

	public string? Body { get; set; }
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