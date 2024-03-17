using System.IdentityModel.Tokens.Jwt;
using System.Net;
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
    private readonly NotificationMessageTemplates _notificationMessageTemplates;
    private readonly IMailService _mailService;
    private readonly IMapper _mapper;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        JwtTokenGenerator jwtTokenGenerator,
        IMailService mailService,
        IMapper mapper,
        NotificationMessageTemplates notificationMessageTemplates)
    {
        _userManager = userManager;
        _mapper = mapper;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _mailService = mailService;
        _notificationMessageTemplates = notificationMessageTemplates;
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
        authModel.Email = user.Email;
        authModel.UserName = user.FullName;
        authModel.PhotoUrl = user.PhotoUrl;
        authModel.Roles = userRoles.ToList();
        authModel.Message = "User was authenticated successfully";
        authModel.UserId = user.Id;

        return authModel;
    }

    public async Task<Result<EmailConfirmationResponse>> SendEmailConfirmationLink(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return UserErrors.NotFoundByEmail(email);
        }
        if (user.EmailConfirmed)
        {
            return UserErrors.EmailAlreadyConfirmed(email);
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = generateEmailConfirmationLink(user.Id, token);

        await _mailService.SendEmailAsync(new MailRequest()
        {
            ToEmail = user.Email,
            Subject = "EMAIL CONFIRMATION",
            Body = _notificationMessageTemplates.EmailConfirmation(user.FullName, confirmationLink)
        });

        return new EmailConfirmationResponse
        {
            Email = email,
            Message = "An email confirmation link has been sent to the provided email address"
        };
    }

    private string generateEmailConfirmationLink(string id, string token)
    {
        return
            $"{_signInManager.Context.Request.Scheme}://" +
            $"{_signInManager.Context.Request.Host}" +
            $"/api/auth/confirm-email?" +
            $"id={id}&" +
            $"token={WebUtility.UrlEncode(token)}";
    }

    private string generateResetPasswordLink(string email, string token)
    {
        return
            $"{_signInManager.Context.Request.Scheme}://" +
            $"{_signInManager.Context.Request.Host}/resetPasswordDemo.html?" +
            $"email={WebUtility.UrlEncode(email)}&" +
            $"token={WebUtility.UrlEncode(token)}";
    }

    public async Task<Result<RegisterationResponse>> RegisterAsync(RegistrationRequest request)
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

        await _userManager.AddToRoleAsync(user, request.Role);
        var sendEmailConfirmationResult = await SendEmailConfirmationLink(user.Email!);

        if (sendEmailConfirmationResult.IsFailure)
        {
            return sendEmailConfirmationResult.Error;
        }

        var response = new RegisterationResponse();
        response.Message = $"User: [{request.Email}] has been created successfully\n";
        response.Message += sendEmailConfirmationResult.Value;

        return response;
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
        authModel.UserName = localUserAccount.FullName;
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

        if (user is null)
        {
            return UserErrors.NotFound(id);
        }

        if (user.EmailConfirmed)
        {
            return UserErrors.EmailAlreadyConfirmed(user.Email!);
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

    public async Task<Result<string>> SendResetPasswordLink(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return UserErrors.NotFound(user!.Id);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetPasswordLink = generateResetPasswordLink(email, token);

        await _mailService.SendEmailAsync(new MailRequest
        {
            Subject = "RESET PASSWORD",
            ToEmail = email,
            Body = _notificationMessageTemplates.ResetPassword(resetPasswordLink)
        });

        return "An email with reset password link has been sent to your email address";
    }

    public async Task<Result<string>> ResetPassword(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return UserErrors.NotFoundByEmail(request.Email);
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        var sb = new StringBuilder();
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                sb.AppendLine(error.Code + " : " + error.Description);
            }
            return UserErrors.ValidationErrors(sb.ToString());
        }

        return "Your password has been reset successfully";
    }
}
