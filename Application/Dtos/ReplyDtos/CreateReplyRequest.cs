using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Shared.ReplyDtos;
public class CreateReplyRequest
{
	[Required(ErrorMessage = "Content is required")]
	[MaxLength(2047, ErrorMessage = "Content cannot exceed 2047 characters")]
	public string? Content { get; set; }
}

public class CreateReplyRequestValidator : AbstractValidator<CreateReplyRequest>
{
	public CreateReplyRequestValidator()
	{
		RuleFor(x => x.Content)
			.NotEmpty()
			.NotNull()
			.Must(x => x.Length <= 2047);
	}
}