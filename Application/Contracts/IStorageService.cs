using Microsoft.AspNetCore.Http;
using Shared;

namespace Application.Contracts;
public interface IStorageService
{
    Task<Result<string>> UploadPhoto(IFormFile photo);
}
