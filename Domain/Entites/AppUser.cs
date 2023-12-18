using Domain.Entites;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure;
public class AppUser: IdentityUser
{
    public List<RefreshToken>? RefreshTokens { get; set; }
}
