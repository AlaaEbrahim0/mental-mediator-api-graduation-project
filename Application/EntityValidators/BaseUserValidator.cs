using Domain.Entities;
using FluentValidation;

namespace Application.EntityValidators;

public class BaseUserValidator : AbstractValidator<BaseUser>
{
	public BaseUserValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress().WithMessage("Invalid email address");

		RuleFor(x => x.FirstName)
			.NotEmpty()
			.Length(2, 64).WithMessage("First name must be between 2 and 64 characters");

		RuleFor(x => x.LastName)
			.NotEmpty()
			.Length(2, 64).WithMessage("Last name must be between 2 and 64 characters");

		RuleFor(x => x.BirthDate)
			.NotNull()
			.Must(x => x < DateOnly.FromDateTime(DateTime.Now))
			.WithMessage("Birthdate cannot be in the future");

		RuleFor(x => x.Gender)
			.NotNull()
			.Must(x => x!.Equals("male", StringComparison.InvariantCultureIgnoreCase) || x.Equals("female", StringComparison.InvariantCultureIgnoreCase))
			.WithMessage("Gender must be male or female");

		RuleFor(x => x.PhotoUrl)
			.MaximumLength(2000);

	}
}
