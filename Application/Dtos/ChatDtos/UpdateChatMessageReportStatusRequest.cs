using Domain.Enums;
using FluentValidation;

namespace Application.Dtos.ChatDtos;

public class UpdateChatMessageReportStatusRequest
{
	public ChatReportStatus Status { get; set; }
}

public class UpdateChatMessageReportStatusRequestValidator : AbstractValidator<UpdateChatMessageReportStatusRequest>
{
	public UpdateChatMessageReportStatusRequestValidator()
	{
		RuleFor(x => x.Status)
			.Must(status => status is ChatReportStatus.Reviewed or ChatReportStatus.Dismissed or ChatReportStatus.Actioned)
			.WithMessage("A report can only be marked as Reviewed, Dismissed or Actioned.");
	}
}
