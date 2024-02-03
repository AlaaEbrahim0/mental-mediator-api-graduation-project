using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Services;
using Application.Utilities;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Infrastructure.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.AuthDtos;

namespace Infrastructure.Services;
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IMailService _mailService;
    private readonly IMapper _mapper;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        JwtTokenGenerator jwtTokenGenerator,
        IMailService mailService,
        IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _mailService = mailService;
    }

    public async Task<Result<RegisterationResponse>> RegisterAsync(RegistrationRequest request, Func<string, string, string> callback)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is not null)
        {
            return UserErrors.EmailNotUnique(request.Email);
        }

        user = _mapper.Map<AppUser>(request);
        var createUserResult = await _userManager.CreateAsync(user, request.Password);

        var sb = new StringBuilder();
        if (!createUserResult.Succeeded)
        {
            foreach (var error in createUserResult.Errors)
            {
                sb.AppendLine(error.Code + " : " + error.Description);
            }
            return UserErrors.ValidationErrors(sb.ToString());
        }

        await _userManager.AddToRoleAsync(user, "User");
        await SendEmailConfirmationLink(user, callback);

        var response = new RegisterationResponse();
        response.Message = $"User: [{request.Email}] has been created succesfully";
        response.Email = request.Email;

        return response;
    }

    public async Task<Result<AuthResponse>> SignInAsync(SignInRequest signInModel)
    {
        var authModel = new AuthResponse();

        var user = await _userManager.FindByEmailAsync(signInModel.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, signInModel.Password))
        {
            return UserErrors.InvalidCredentials();
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            return UserErrors.EmailNotConfirmed();
        }

        var token = await _jwtTokenGenerator.CreateJwtToken(user);
        var userRoles = await _userManager.GetRolesAsync(user!);

        authModel.Token = new JwtSecurityTokenHandler().WriteToken(token);
        authModel.ExpiresOn = token.ValidTo;
        authModel.Email = user!.Email;
        authModel.Roles = userRoles.ToList();
        authModel.Message = "User was authenticated successfully, We send an email confirmation link to your email address";
        authModel.UserId = user.Id;

        return authModel;
    }

    private async Task SendEmailConfirmationLink(AppUser user, Func<string, string, string> callback)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = callback.Invoke(user.Id, token);

        await _mailService.SendEmailAsync(new MailRequest()
        {
            ToEmail = user.Email,
            Subject = "EMAIL CONFIRMATION",
            Body = NotificationMessageTemplates.EmailConfirmationMessage(user.UserName!, confirmationLink)
        });
    }

    public async Task<Result<AuthResponse>> ExternalLoginAsync()
    {
        var authModel = new AuthResponse();

        var externalUserInfo = await _signInManager.GetExternalLoginInfoAsync();
        if (externalUserInfo is null)
        {
            return Error.Validation(
            "ExternalAuthentication.RemoteError",
              "Error loading external login information"
             );
        }

        var localUserAccount = await _userManager.FindByEmailAsync(externalUserInfo.Principal.FindFirstValue(ClaimTypes.Email)!);

        if (localUserAccount is null)
        {
            var externalUserEmail = externalUserInfo.Principal.FindFirstValue(ClaimTypes.Email);

            var username = externalUserEmail!.Split('@')[0];
            var name = externalUserInfo.Principal.Identity?.Name;

            localUserAccount = new AppUser()
            {
                Email = externalUserEmail,
                UserName = username,
                FirstName = name,
            };

            await _userManager.CreateAsync(localUserAccount);
            await _userManager.AddToRoleAsync(localUserAccount, "User");
            localUserAccount.EmailConfirmed = true;
        }
        var token = await _jwtTokenGenerator.CreateJwtToken(localUserAccount);

        var roles = await _userManager.GetRolesAsync(localUserAccount);

        authModel.Email = localUserAccount.Email;
        authModel.UserId = localUserAccount.Id;
        authModel.Token = new JwtSecurityTokenHandler().WriteToken(token);
        authModel.ExpiresOn = token.ValidTo;
        authModel.Roles = roles.ToList();
        authModel.Message = $"User: [{localUserAccount.Email}] has been created and confirmed succesfully";

        return authModel;
    }

    public AuthenticationProperties GetExternalAuthenticationProperties(string provider, string? redirectUrl)
    {
        return _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
    }

    public async Task<Result<EmailConfirmationResponse>> ConfirmEmailAsync(string id, string token)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return UserErrors.NotFound(id);
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
        {
            var sb = new StringBuilder();
            foreach (var error in result.Errors)
            {
                sb.AppendLine(error.Description);
            }
            return UserErrors.InvalidToken(sb.ToString());
        }

        return new EmailConfirmationResponse()
        {
            Email = user.Email,
            Message = "Email has been confirmed successfully"
        };
    }

}
