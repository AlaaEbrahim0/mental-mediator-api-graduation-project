using Domain.Entities;
using FluentValidation;

namespace Application.EntityValidators;

public class NotificationValidator : AbstractValidator<Notification>
{
	public NotificationValidator()
	{
		RuleFor(x => x.Message)
			.NotEmpty()
			.Length(8, 400);

		RuleFor(x => x.AppUserId)
			.NotEmpty();
	}
}
