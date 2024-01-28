using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Options;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Utilities;
public class JwtTokenGenerator
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(UserManager<AppUser> userManager, IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<JwtSecurityToken> CreateJwtToken(AppUser user)
    {
        IEnumerable<Claim> jwtClaims = await GetUserJwtClaims(user);

        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key!)); ;
        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken
        (
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: jwtClaims,
            expires: DateTime.Now.AddDays(_jwtOptions.Duration),
            signingCredentials: signingCredentials
        );

        return token;
    }

    private async Task<IEnumerable<Claim>> GetUserJwtClaims(AppUser user)
    {
        var userClaims = await _userManager.GetClaimsAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var roleClaims = new List<Claim>();

        foreach (var role in roles)
        {
            roleClaims.Add(new Claim("roles", role));
        }

        var jwtClaims = new[]
        {
            new Claim("uid", user.Id!),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName!),
            new Claim(JwtRegisteredClaimNames.Jti, user.Email!, Guid.NewGuid().ToString()),
        }
        .Union(userClaims)
        .Union(roleClaims);

        return jwtClaims;
    }
}
