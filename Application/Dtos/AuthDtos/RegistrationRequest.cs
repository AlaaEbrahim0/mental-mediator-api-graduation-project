using System.ComponentModel;
using FluentValidation;

namespace Application.Dtos.AuthDtos;

public class RegisterationRequest
{

	[DefaultValue("name")]
	public string? FirstName { get; set; }

	[DefaultValue("name")]
	public string? LastName { get; set; }

	public string Email { get; init; } = string.Empty;

	[DefaultValue("Password1!")]
	public string Password { get; init; } = string.Empty;

	public DateOnly BirthDate { get; set; }

	[DefaultValue("male")]
	public string Gender { get; set; } = string.Empty;

	public string Username => Email.Split('@')[0];

	[DefaultValue("User")]
	public string Role { get; set; } = string.Empty;
}


public class RegisterationRequestValidator : AbstractValidator<RegisterationRequest>
{
	public RegisterationRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress().WithMessage("Invalid email address");

		RuleFor(x => x.Password)
			.NotEmpty()
			.Length(8, 128).WithMessage("Password must be between 8 and 128 characters");

		RuleFor(x => x.FirstName)
			.NotEmpty()
			.Length(2, 64).WithMessage("First name must be between 2 and 64 characters");

		RuleFor(x => x.LastName)
			.NotEmpty()
			.Length(2, 64).WithMessage("Last name must be between 2 and 64 characters");

		RuleFor(x => x.BirthDate)
			.NotEmpty()
			.Must(x => x < DateOnly.FromDateTime(DateTime.Now))
			.WithMessage("Birthdate cannot be in the future");

		RuleFor(x => x.Gender)
			.NotNull()
			.Must(x => x.Equals("male", StringComparison.InvariantCultureIgnoreCase) ||
			x.Equals("female", StringComparison.InvariantCultureIgnoreCase))
			.WithMessage("Gender must be male or female");

		RuleFor(x => x.Role)
			.NotNull()
			.Must(x => x.Equals("user", StringComparison.OrdinalIgnoreCase) ||
			x.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
			x.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
			.WithMessage("Role must be user or admin or doctor");


	}
}