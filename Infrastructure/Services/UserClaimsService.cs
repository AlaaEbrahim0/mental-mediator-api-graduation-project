using Application.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Infrastructure.Services;
public class UserClaimsService : IUserClaimsService
{
    private readonly SignInManager<BaseUser> _signInManager;

    public UserClaimsService(SignInManager<BaseUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public string GetUserId()
    {
        return _signInManager.Context.User.FindFirst("uid")?.Value
            ?? _signInManager.Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;
    }

    public string GetUserName()
    {
        return _signInManager.Context.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value
            ?? string.Empty;
    }

    public string GetPhotoUrl()
    {
        return _signInManager.Context.User.FindFirst("PhotoUrl")?.Value
            ?? string.Empty;
    }

    public string GetEmail()
    {
        return _signInManager.Context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? _signInManager.Context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? string.Empty;
    }

    public string GetRole()
    {
        var role = _signInManager.Context.User.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
            c.Type == "roles")?.Value;

        return role ?? string.Empty;
    }
}
