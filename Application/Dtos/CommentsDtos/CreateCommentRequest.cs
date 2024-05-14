using System.ComponentModel;
using FluentValidation;

namespace Shared.CommentsDtos;
public class CreateCommentRequest
{
	[DefaultValue("Comment Example")]
	public string Content { get; set; } = string.Empty;
}

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
	public CreateCommentRequestValidator()
	{
		RuleFor(x => x.Content)
			.NotNull()
			.Length(1, 2000);
	}
}