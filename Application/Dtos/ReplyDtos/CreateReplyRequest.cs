using FluentValidation;

namespace Application.Dtos.ReplyDtos;
public class CreateReplyRequest
{
	public string? Content { get; set; }
}

public class CreateReplyRequestValidator : AbstractValidator<CreateReplyRequest>
{
	public CreateReplyRequestValidator()
	{
		RuleFor(x => x.Content)
			.NotEmpty()
			.NotNull()
			.Length(1, 2000);
	}
}