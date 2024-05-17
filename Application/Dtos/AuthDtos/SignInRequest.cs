using System.ComponentModel;
using FluentValidation;

namespace Application.Dtos.AuthDtos;
public record SignInRequest
{
	[DefaultValue("test@example.com")]
	public string Email { get; init; } = string.Empty;

	[DefaultValue("Password1!")]
	public string Password { get; init; } = string.Empty;
}

public class SignInRequestValidator : AbstractValidator<SignInRequest>
{
	public SignInRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress().WithMessage("Invalid email address");

		RuleFor(x => x.Password)
			.NotEmpty()
			.Length(8, 128).WithMessage("Password must be between 8 and 128 characters");
	}

}