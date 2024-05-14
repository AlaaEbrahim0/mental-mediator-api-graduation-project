using FluentValidation;

namespace Application.Dtos.WeeklyScheduleDtos;


public class CreateAvailableDayRequest : UpdateAvailableDayRequest
{
	public DayOfWeek DayOfWeek { get; set; }
}

public class CreateAvailableDayRequestValidator : AbstractValidator<CreateAvailableDayRequest>
{
	public CreateAvailableDayRequestValidator()
	{
		RuleFor(x => x.DayOfWeek)
			.NotNull();
	}
}
