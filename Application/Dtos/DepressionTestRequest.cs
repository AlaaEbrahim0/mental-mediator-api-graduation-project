using FluentValidation;

namespace Application.Dtos;
public class DepressionTestRequest
{
	public string Text { get; set; } = string.Empty;
	public int Sum { get; set; }
}

public class DepressionTestRequestValidator : AbstractValidator<DepressionTestRequest>
{
	public DepressionTestRequestValidator()
	{
		RuleFor(x => x.Text)
			.NotEmpty()
			.Length(1, 20000);
	}
}
