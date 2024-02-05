using Microsoft.AspNetCore.Authentication;
using Shared;
using Shared.AuthDtos;

namespace Application.Services;
public interface IAuthService
{
    Task<Result<RegisterationResponse>> RegisterAsync(RegistrationRequest RegistrationRequest, Func<string, string, string> callback);
    Task<Result<AuthResponse>> SignInAsync(SignInRequest signInModel);
    Task<Result<AuthResponse>> ExternalLoginAsync();
    Task<Result<EmailConfirmationResponse>> ConfirmEmailAsync(string id, string token);
    AuthenticationProperties GetExternalAuthenticationProperties(string provider, string? redirectUrl);
}

