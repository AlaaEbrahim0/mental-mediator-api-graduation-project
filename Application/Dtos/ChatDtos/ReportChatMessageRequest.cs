using FluentValidation;

namespace Application.Dtos.ChatDtos;

public class ReportChatMessageRequest
{
	public string Reason { get; set; } = string.Empty;
}

public class ReportChatMessageRequestValidator : AbstractValidator<ReportChatMessageRequest>
{
	public ReportChatMessageRequestValidator()
	{
		RuleFor(x => x.Reason)
			.NotEmpty()
			.Length(1, 1000);
	}
}
