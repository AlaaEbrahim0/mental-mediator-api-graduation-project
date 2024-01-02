using System.Security.Claims;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Shared;

namespace Application.Services;
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegistrationModel registrationModel, Func<string, string, string> callback);
    Task<AuthResponse> SignInAsync(SignInModel signInModel);
    Task<AuthResponse> ExternalLoginAsync();
    AuthenticationProperties GetExternalAuthenticationProperties(string provider, string? redirectUrl);
    Task<EmailConfirmationResponse> ConfirmEmailAsync(string id, string token);  
}

