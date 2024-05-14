using Domain.Entities;
using FluentValidation;

namespace Application.EntityValidators;
public class PostValidator : AbstractValidator<Post>
{
	public PostValidator()
	{
		RuleFor(x => x.Title)
			.Length(1, 500);

		RuleFor(x => x.Content)
			.Length(1, 2000);

		RuleFor(x => x.AppUserId)
			.NotEmpty();
	}
}
