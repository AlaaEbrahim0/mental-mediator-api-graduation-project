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
		return _signInManager.Context.User.FindFirst("uid")!.Value;
	}

	public string GetUserName()
	{
		return _signInManager.Context.User.FindFirst(JwtRegisteredClaimNames.Name)!.Value;
	}
	public string GetPhotoUrl()
	{
		return _signInManager.Context.User.FindFirst("PhotoUrl")!.Value;
	}

	public string GetRole()
	{
		var role = _signInManager.Context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")!.Value;

		return role;
	}


}
