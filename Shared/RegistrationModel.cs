using System.ComponentModel.DataAnnotations;

namespace Shared;

public record RegistrationModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; init; } = string.Empty;


    public string Username => Email.Split('@')[0];
}
