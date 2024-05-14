using Domain.Entities;
using FluentValidation;

namespace Application.EntityValidators;

public class ReplyValidator : AbstractValidator<Comment>
{
	public ReplyValidator()
	{
		RuleFor(x => x.Content)
			.NotNull()
			.Length(1, 2000);

		RuleFor(x => x.AppUserId)
			.NotNull()
			.NotEmpty();
	}
}
