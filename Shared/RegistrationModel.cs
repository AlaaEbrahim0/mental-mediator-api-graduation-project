using System.ComponentModel.DataAnnotations;

namespace Shared;

public record RegistrationModel
{

    [Required(ErrorMessage = "First name is required.")]
    [MinLength(3, ErrorMessage = "First name cannot be less than 3 characters")]
    [MaxLength(44, ErrorMessage = "First name cannot be greater than 44 characters")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required.")]
    [MinLength(3, ErrorMessage = "Last name cannot be less than 3 characters")]
    [MaxLength(44, ErrorMessage = "Last name cannot be greater than 44 characters")]
    public string? LastName { get; set; }
    
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(byte.MaxValue, ErrorMessage = "Email cannot be greater than 255 character")]
    [MinLength(12, ErrorMessage = "Email cannot be less than 3 characters")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Birthdate is required")]
    public DateOnly BirthDate { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [AllowedValues("Male", "Female")]
    public string? Gender { get; set; }

    public string Username => Email.Split('@')[0];
}
