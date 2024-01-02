using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Services;
using Application.Utilities;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Shared;

namespace Infrastructure.Services;
public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly IMailService _mailService;
    private readonly IMapper _mapper;

    public AuthService(UserManager<AppUser> userManager, IMapper mapper, SignInManager<AppUser> signInManager, JwtTokenGenerator jwtTokenGenerator, IMailService mailService)
    {
        _userManager = userManager;
        _mapper = mapper;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _mailService = mailService;
    }

    public async Task<AuthResponse> RegisterAsync (RegistrationModel model, Func<string, string, string> callback)
    {
        var response = new AuthResponse();

        var emailExist = await FindByEmail(model.Email);

        if (emailExist)
        {
            response.Message = "Email already exist";
            return response;
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

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var confirmationLink = callback.Invoke(user.Id, encodedToken);

        await _mailService.SendEmailAsync(new MailRequest()
        {
            ToEmail = user.Email,
            Subject = "EMAIL CONFIRMATION",
            Body = NotificationMessageTemplates.EmailConfirmationMessage(user.UserName!, confirmationLink)
        });

        response.Message = $"User: [{model.Email}] has been created succesfully";
        response.Email = model.Email;
        response.Roles = new List<string> { "User" };
        response.IsAuthenticated = true;
        
        return response;
    }

    private async Task<bool> FindByEmail(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? false : true;
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

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            authModel.Message = "Email has not been confirmed yet";
            authModel.IsAuthenticated = false;
            return authModel;
        }

        var token = await _jwtTokenGenerator.CreateJwtToken(user);
        var userRoles = await _userManager.GetRolesAsync(user!);

        authModel.IsAuthenticated = true;
        authModel.Token = new JwtSecurityTokenHandler().WriteToken(token);
        authModel.ExpiresOn = token.ValidTo;
        authModel.Email = user!.Email;
        authModel.Roles = userRoles.ToList();
        authModel.Message = "Successful sign in";

        return authModel;
    }

    public async Task<AuthResponse> ExternalLoginAsync()
    {
        var authModel = new AuthResponse();

        var externalUserInfo = await _signInManager.GetExternalLoginInfoAsync();
        if (externalUserInfo is null)
        {
            authModel.Message = "Error loading external login information";
            authModel.IsAuthenticated = false;
            return authModel;
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
        }
        authModel.Email = localUserAccount.Email;
        authModel.Message = $"User: [{localUserAccount.Email}] has been created succesfully";

        return authModel;
    }

    public AuthenticationProperties GetExternalAuthenticationProperties(string provider, string? redirectUrl)
    {
        return _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
    }

    public async Task<EmailConfirmationResponse> ConfirmEmailAsync(string id, string token)
    {
        var responseDto = new EmailConfirmationResponse();
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            responseDto.IsSuccessful = false;
            responseDto.Message = $"Unable to load user with ID '{id}'.";
            return responseDto;
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (result.Succeeded)
        {
            responseDto.IsSuccessful = true;
            responseDto.Message = "Email confirmed successfully";
        }
        else
        {
            responseDto.IsSuccessful = false;
            responseDto.Message = "Error confirming email";
        }

        return responseDto;
    }

}
