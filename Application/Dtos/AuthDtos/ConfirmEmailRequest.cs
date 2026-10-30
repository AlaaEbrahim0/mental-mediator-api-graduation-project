using FluentValidation;

namespace Application.Dtos.AuthDtos;

public class ConfirmEmailRequest
{
	public string Id { get; init; } = string.Empty;
	public string Token { get; init; } = string.Empty;
}

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
	public ConfirmEmailRequestValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty()
			.MaximumLength(450);

		RuleFor(x => x.Token)
			.NotEmpty()
			.MaximumLength(4096);
	}
}
