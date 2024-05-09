using System.ComponentModel.DataAnnotations;

namespace Shared;
public class ValidateEnumAttribute : ValidationAttribute
{
	protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
	{
		var enumType = value!.GetType();
		bool isValidEnumValue = Enum.IsDefined(enumType, value);

		if (!isValidEnumValue)
		{
			var allowedValues = Enum.GetNames(enumType);
			var allowedValuesString = string.Join(", ", allowedValues);
			return new ValidationResult($"Invalid value. Please use one of the following values: {allowedValuesString}.");
		}

		return ValidationResult.Success!;
	}
}