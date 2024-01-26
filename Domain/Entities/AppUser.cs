using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;
public class AppUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public string? Gender { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public override string? NormalizedEmail { get => base.Email!.ToUpper(); }
    public override string? NormalizedUserName { get => base.UserName!.ToUpper(); }

    public override string? UserName => Email;

    public List<Post> Posts { get; set; } = new();
}


