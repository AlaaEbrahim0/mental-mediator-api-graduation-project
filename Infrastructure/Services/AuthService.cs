using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Abstractions;
using Application.Services;
using Application.Utilities;
using AutoMapper;
using Domain.Entities ;
using Infrastructure.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Shared;

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

    public async Task<Result<RegisterationResponse>> RegisterAsync(RegistrationRequest model, Func<string, string, string> callback)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user is not null)
        {
            return new Error(
                "Registeration.EmailAlreadyExist",
                 "User with the same email address already exists"
                );
        }

        user = _mapper.Map<AppUser>(model);
        var createUserResult = await _userManager.CreateAsync(user, model.Password);

        var sb = new StringBuilder();
        if (!createUserResult.Succeeded)
        {
            foreach (var error in createUserResult.Errors)
            {
                sb.AppendLine(error.Code + " : " + error.Description);
            }
            return new Error("Registration.IdentityErrors", sb.ToString());
        }

        await _userManager.AddToRoleAsync(user, "User");
        await SendEmailConfirmationLink(user, callback);

        var response = new RegisterationResponse();
        response.Message = $"User: [{model.Email}] has been created succesfully";
        response.Email = model.Email;

        return response;
    }

    public async Task<Result<AuthResponse>> SignInAsync(SignInRequest signInModel)
    {
        var authModel = new AuthResponse();

        var user = await _userManager.FindByEmailAsync(signInModel.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, signInModel.Password))
        {
            return new Error(
                "Authentication.InvalidCredentials",
                "Invalid Email Address or Password"
            );
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            return new Error(
                "Authentication.UnconfirmedEmail",
                "Email has not been confirmed yet"
            );
        }

        var token = await _jwtTokenGenerator.CreateJwtToken(user);
        var userRoles = await _userManager.GetRolesAsync(user!);

        authModel.Token = new JwtSecurityTokenHandler().WriteToken(token);
        authModel.ExpiresOn = token.ValidTo;
        authModel.Email = user!.Email;
        authModel.Roles = userRoles.ToList();
        authModel.Message = "User was authenticated successfully";

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
            return new Error(
         "ExternalAuthentication.RemoteError",
              "Error loading external login information"
             );
        }

        var localUserAccount = await _userManager.FindByEmailAsync(externalUserInfo.Principal.FindFirstValue(ClaimTypes.Email)!);

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
            localUserAccount.EmailConfirmed = true;
        }

        authModel.Email = localUserAccount.Email;
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
            return new Error("EmailConfirmation", $"Unable to load user with ID '{id}'.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
        {
            var sb = new StringBuilder();
            foreach (var error in result.Errors)
            {
                sb.Append(error.Code + " : " + error.Description);
            }
            return new Error("EmailConfirmation.IdentityErrors", sb.ToString());
        }

        return new EmailConfirmationResponse()
        {
            Email = user.Email,
            Message = "Email has been confirmed successfully"
        };
    }

}
