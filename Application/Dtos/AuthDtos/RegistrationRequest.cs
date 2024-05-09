using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Shared;

namespace Application.Dtos.AuthDtos;

public record RegisterationRequest
{

	[Required(ErrorMessage = "First name is required.")]
	[MinLength(3, ErrorMessage = "First name cannot be less than 3 characters")]
	[MaxLength(44, ErrorMessage = "First name cannot be greater than 44 characters")]
	[DefaultValue("name")]
	public string? FirstName { get; set; }

	[Required(ErrorMessage = "Last name is required.")]
	[MinLength(3, ErrorMessage = "Last name cannot be less than 3 characters")]
	[MaxLength(44, ErrorMessage = "Last name cannot be greater than 44 characters")]
	[DefaultValue("name")]
	public string? LastName { get; set; }

	[Required(ErrorMessage = "Email is required.")]
	[RegularExpression("[A-Za-z0-9]+@[A-Za-z]+\\.[A-Za-z]+", ErrorMessage = "Invalid email address format")]
	[DefaultValue("test@example.com")]
	public string Email { get; init; } = string.Empty;

	[Required(ErrorMessage = "Password is required.")]
	[StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
	[DefaultValue("Password1!")]
	public string Password { get; init; } = string.Empty;

	[Required(ErrorMessage = "Birthdate is required")]
	[FutureDateTimeValidation()]
	[DefaultValue("2000-01-01")]
	public DateOnly BirthDate { get; set; }

	[Required(ErrorMessage = "Gender is required")]
	[AllowedValues("male", "female", ErrorMessage = "Gender must have a value of [male, female]")]
	[DefaultValue("male")]
	public string? Gender { get; set; }

	public string Username => Email.Split('@')[0];

	[Required(ErrorMessage = "Role is required")]
	[AllowedValues("User", "Admin", "Doctor", ErrorMessage = "Allowed roles are [User, Doctor, Admin]")]
	[DefaultValue("User")]
	public string Role { get; set; } = string.Empty;
}


