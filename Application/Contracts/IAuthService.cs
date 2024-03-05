using Microsoft.AspNetCore.Authentication;
using Shared;
using Shared.AuthDtos;

namespace Application.Services;
public interface IAuthService
{
    Task<Result<RegisterationResponse>> RegisterAsync(RegistrationRequest RegistrationRequest);
    Task<Result<string>> SendResetPasswordLink(string email);
    Task<Result<AuthResponse>> SignInAsync(SignInRequest signInModel);
    Task<Result<EmailConfirmationResponse>> SendEmailConfirmationLink(string email);
    Task<Result<AuthResponse>> ExternalLoginAsync();
    Task<Result<EmailConfirmationResponse>> ConfirmEmailAsync(string id, string token);
    AuthenticationProperties GetExternalAuthenticationProperties(string provider, string? redirectUrl);
    Task<Result<string>> ResetPassword(ResetPasswordRequest request);
}

