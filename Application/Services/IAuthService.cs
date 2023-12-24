using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace Application.Services;
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegistrationModel registrationModel);
    Task<AuthResponse> SignInAsync(SignInModel registrationModel);
    Task<AuthResponse> AddExternalLoginAsync();
}

