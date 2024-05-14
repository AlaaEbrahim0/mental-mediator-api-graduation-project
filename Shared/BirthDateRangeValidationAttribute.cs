using System.ComponentModel.DataAnnotations;

namespace Shared;

public class BirthDateRangeValidationAttribute : ValidationAttribute
{
	public DateOnly MaxDate { get; set; }
		= DateOnly.FromDateTime(new DateTime(2014, 1, 1));
	public DateOnly MinDate { get; set; }
		= DateOnly.FromDateTime(new DateTime(1950, 1, 1));

	public override bool IsValid(object? obj)
	{
		var value = (DateOnly)obj!;
		if (!(value > MinDate && value < MaxDate))
		{
			ErrorMessage = "Date cannot be before 1950 or after 2014";
			return false;
		};
		return true;
	}
}
