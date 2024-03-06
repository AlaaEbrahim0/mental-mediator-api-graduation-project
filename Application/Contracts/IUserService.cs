using Shared;
using Shared.UserDtos;

namespace Application.Contracts;
public interface IUserService
{
    Task<Result<UserInfoResponse>> GetUserInfo(string id);
    Task<Result<UserInfoResponse>> UpdateUserInfo(string id, UpdateUserInfoRequest request);

}
