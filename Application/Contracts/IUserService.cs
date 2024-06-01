using Application.Dtos.UserDtos;
using Shared;

namespace Application.Contracts;
public interface IUserService
{
	Task<Result<UserInfoResponse>> GetUserInfo(string id);
	Task<Result<UserInfoResponse>> GetCurrentUserInfo();
	Task<Result<UserInfoResponse>> UpdateUserInfo(string id, UpdateUserInfoRequest request);
	Task<Result<UserInfoResponse>> UpdateCurrentUserInfo(UpdateUserInfoRequest updateRequest);
}

