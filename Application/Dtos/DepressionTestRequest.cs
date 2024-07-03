using FluentValidation;

namespace Application.Dtos;
public class DepressionTestRequest
{
	public string Text { get; set; } = string.Empty;
	public int Sometimes { get; set; }
	public int Always { get; set; }
	public int Never { get; set; }
	public int Usually { get; set; }
}

public class DepressionTestRequestValidator : AbstractValidator<DepressionTestRequest>
{
	public DepressionTestRequestValidator()
	{
		RuleFor(x => x.Text)
			.NotEmpty()
			.Length(1, 4000);
	}
}
