using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Shared.UserDtos;
public class UpdateUserInfoRequest
{
	[Required(ErrorMessage = "First name is required.")]
	[MinLength(3, ErrorMessage = "First name cannot be less than 3 characters")]
	[MaxLength(44, ErrorMessage = "First name cannot be greater than 44 characters")]
	public string FirstName { get; set; } = string.Empty;

	[Required(ErrorMessage = "Last name is required.")]
	[MinLength(3, ErrorMessage = "Last name cannot be less than 3 characters")]
	[MaxLength(44, ErrorMessage = "Last name cannot be greater than 44 characters")]
	public string LastName { get; set; } = string.Empty;

	[Required(ErrorMessage = "Birthdate is required")]
	public DateOnly BirthDate { get; set; }

	[Required(ErrorMessage = "Gender is required")]
	[AllowedValues("male", "female", ErrorMessage = "Gender must have a value of [male, female]")]
	public string Gender { get; set; } = string.Empty;

	public IFormFile? Photo { get; set; }
}


public class UpdateDoctorInfoRequest : UpdateUserInfoRequest
{
	public string? Biography { get; set; }
}

