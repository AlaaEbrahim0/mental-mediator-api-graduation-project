using System.Diagnostics.Tracing;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using Application.Options;
using Application.Services;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared;

namespace Infrastructure.Services;
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtOptions _jwtOptions;
    private readonly IMailService _mailService;
    private readonly IMapper _mapper;

    public AuthService(UserManager<AppUser> userManager, IMapper mapper, IOptions<JwtOptions> jwtOptions, SignInManager<AppUser> signInManager, IMailService mailService)
    {
        _userManager = userManager;
        _mapper = mapper;
        _jwtOptions = jwtOptions.Value;
        _signInManager = signInManager;
        _mailService = mailService;
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
            foreach (var error in result.Errors)
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

    public async Task SendEmailConfirmationMessage(string email, string confirmationLink)
    {
        var user = await _userManager.FindByEmailAsync(email);
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        confirmationLink += $"&token={WebUtility.UrlEncode(token)}";
        var mailRequest = new MailRequest();
        mailRequest.ToEmail = email;
        mailRequest.Subject = "Email Confirmation";
        var message = EmailConfirmationMessageTemplate.GenerateConfirmationEmail(mailRequest.ToEmail, confirmationLink);
        mailRequest.Body = message;
        await _mailService.SendEmailAsync(mailRequest);
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
            return authModel;
        }

        if (!user.EmailConfirmed)
        {
            authModel.Message = "Email isn't confirmed";
            authModel.IsAuthenticated = false;
            return authModel;
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

    public async Task<AuthResponse> AddExternalLoginAsync()
    {
        var authModel = new AuthResponse();

        var externalUserInfo = await _signInManager.GetExternalLoginInfoAsync();
        if (externalUserInfo is null)
        {
            authModel.Message = "Error loading external login information";
            authModel.IsAuthenticated = false;
        }

        var localUserAccount = await _userManager.FindByEmailAsync(externalUserInfo!.Principal.FindFirstValue(ClaimTypes.Email)!);

        if (localUserAccount is null)
        {
            var externalUserEmail = externalUserInfo.Principal.FindFirstValue(ClaimTypes.Email);
            localUserAccount = new AppUser()
            {
                Email = externalUserEmail,
                UserName = externalUserEmail!.Split('@')[0]
            };
            await _userManager.CreateAsync(localUserAccount);
            await _userManager.AddToRoleAsync(localUserAccount, "User");
        }

        await _userManager.AddLoginAsync(localUserAccount, externalUserInfo);
        await _signInManager.ExternalLoginSignInAsync(externalUserInfo.LoginProvider, externalUserInfo.ProviderKey, false, true);

        var token = await CreateJwtToken(localUserAccount);
        authModel.Token = new JwtSecurityTokenHandler().WriteToken(token);
        authModel.Email = localUserAccount.Email;
        authModel.ExpiresOn = token.ValidTo;
        authModel.IsAuthenticated = true;
        authModel.Roles = (await _userManager.GetRolesAsync(localUserAccount)).ToList();
        authModel.Message = "Successful Login";

        return authModel;

    }

    public async Task<string> ConfirmEmail(string email, string token)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return $"Confirmation Failed\nERROR: user: {email} doesn't exist";
        }

        var actualToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var result = await _userManager.ConfirmEmailAsync(user, token);
        var sb = new StringBuilder();
        if (!result.Succeeded)
        {
            result.Errors.Select(e => sb.AppendLine(e.Description));
            return sb.ToString();
        }
        return "Email has been confirmed successfully";
    }
}
