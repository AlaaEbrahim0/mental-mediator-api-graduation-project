using FluentValidation;

namespace Application.Dtos.AuthDtos;
public class ResetPasswordRequest
{
	public string Email { get; set; } = string.Empty;
	public string Token { get; set; } = string.Empty;
	public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
	public ResetPasswordRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotNull()
			.NotEmpty()
			.EmailAddress().WithMessage("Invalid email address");

		RuleFor(x => x.Token)
			.NotNull()
			.NotEmpty();

		RuleFor(x => x.NewPassword)
			.NotNull()
			.NotEmpty()
			.Length(8, 128).WithMessage("Password must be between 8 and 128 characters");
	}
}