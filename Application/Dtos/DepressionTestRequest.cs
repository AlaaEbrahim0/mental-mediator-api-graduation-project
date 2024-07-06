using Domain.Enums;
using FluentValidation;

namespace Application.Dtos;
public class DepressionTestRequest
{
	public string Text { get; set; } = string.Empty;
	public int Sum { get; set; }
	public Gender Gender { get; set; }
	public int Age { get; set; }

}

public class DepressionTestResponse
{
	public string? Prediction { get; set; }
}

public class DepressionTestRequestValidator : AbstractValidator<DepressionTestRequest>
{
	public DepressionTestRequestValidator()
	{
		RuleFor(x => x.Text)
			.NotEmpty()
			.Length(1, 20000);

		RuleFor(x => x.Age)
			.NotEmpty()
			.GreaterThanOrEqualTo(1)
			.LessThanOrEqualTo(100);

	}
}
