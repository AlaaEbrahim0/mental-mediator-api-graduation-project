using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Application.Dtos.AuthDtos;
public record SignInRequest
{
	[Required(ErrorMessage = "Email is required")]
	[EmailAddress(ErrorMessage = "Invalid email address format")]
	[DefaultValue("test@example.com")]
	public string Email { get; init; } = string.Empty;

	[Required(ErrorMessage = "Password is required")]
	[DefaultValue("Password1!")]
	public string Password { get; init; } = string.Empty;
}
