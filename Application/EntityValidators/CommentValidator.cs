using Domain.Entities;
using FluentValidation;

namespace Application.EntityValidators;

public class CommentValidator : AbstractValidator<Comment>
{
	public CommentValidator()
	{
		RuleFor(x => x.Content)
			.NotNull()
			.Length(1, 2000);

		RuleFor(x => x.AppUserId)
			.NotNull()
			.NotEmpty();
	}
}
