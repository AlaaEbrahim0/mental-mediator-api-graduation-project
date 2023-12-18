using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Application.Options;
using Application.Services;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared;

namespace Infrastructure;
public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtOptions _jwtOptions;
    private readonly IMapper _mapper;

    public AuthenticationService(UserManager<AppUser> userManager, IMapper mapper, IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _mapper = mapper;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegistrationModel model)
    {
        var response = new AuthResponse();

        var emailExist = await FindByEmail(model.Email);

        if (emailExist)
        {
            response.Message = "Email already exist";
        }
       
        var user = _mapper.Map<AppUser>(model);
        var result = await _userManager.CreateAsync(user, model.Password);

        var sb = new StringBuilder();
        if (!result.Succeeded)
        {
            foreach(var error in  result.Errors)
            {
                sb.AppendLine(error.Code + " : " + error.Description);
            }
            response.Message = sb.ToString();
            return response;
        }
        
        await _userManager.AddToRoleAsync(user, "User");
            
        var token = await CreateJwtToken(user);

        response.Message = $"User: [{model.Email}] has been created succesfully";
        response.IsAuthenticated = true;
        response.Token = new JwtSecurityTokenHandler().WriteToken(token);
        response.ExpiresOn = token.ValidTo; 
        response.Email = model.Email;
        response.Roles = new List<string> { "User" };


        return response;
    }

    private async Task<bool> FindByEmail(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? false : true;
    }

    private async Task<JwtSecurityToken> CreateJwtToken(AppUser user)
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
            new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, user.Email!, Guid.NewGuid().ToString()),
            new Claim("uid", user.Id),
        }
        .Union(userClaims)
        .Union(roleClaims);

        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
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

    public async Task<AuthResponse> SignInAsync(SignInModel signInModel)
    {
        var authModel = new AuthResponse();

        var user = await _userManager.FindByEmailAsync(signInModel.Email);
        
        if (user is null || !await _userManager.CheckPasswordAsync(user, signInModel.Password))
        {
            authModel.Message = "Invalid Email Address or Password";
            authModel.IsAuthenticated = false;
        }

        var token = await CreateJwtToken(user!);
        var userRoles = await _userManager.GetRolesAsync(user!);

        authModel.IsAuthenticated = true;
        authModel.Token = new JwtSecurityTokenHandler().WriteToken(token);
        authModel.ExpiresOn = token.ValidTo;
        authModel.Email = user!.Email;
        authModel.Roles = userRoles.ToList();

        return authModel;
    }
}
