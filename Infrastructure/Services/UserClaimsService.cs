using Application.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Infrastructure.Services;
public class UserClaimsService : IUserClaimsService
{
    private readonly SignInManager<AppUser> _signInManager;

    public UserClaimsService(SignInManager<AppUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public string GetUserId()
    {
        return _signInManager.Context.User.FindFirst("uid")!.Value;
    }

    public string GetUserName()
    {
        return _signInManager.Context.User.FindFirst(JwtRegisteredClaimNames.Name)!.Value;
    }
}
