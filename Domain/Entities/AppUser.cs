using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;
public class AppUser: IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public string? Gender { get; set; }
}


