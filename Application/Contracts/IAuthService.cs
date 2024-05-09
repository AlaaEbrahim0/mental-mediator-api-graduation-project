using Application.Dtos.AuthDtos;
using Microsoft.AspNetCore.Authentication;
using Shared;
namespace Application.Services;
public interface IAuthService
{
	Task<Result<RegisterationResponse>> RegisterAsync(RegisterationRequest RegistrationRequest);
	Task<Result<AuthResponse>> SignInAsync(SignInRequest signInModel);
	Task<Result<EmailConfirmationResponse>> SendEmailConfirmationLink(string email);
	Task<Result<EmailConfirmationResponse>> ConfirmEmailAsync(string id, string token);
	Task<Result<AuthResponse>> ExternalLoginAsync();
	AuthenticationProperties GetExternalAuthenticationProperties(string provider, string? redirectUrl);
	Task<Result<string>> ResetPassword(ResetPasswordRequest request);
	Task<Result<string>> SendResetPasswordLink(string email);
}

