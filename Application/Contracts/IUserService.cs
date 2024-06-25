using Application.Dtos.UserDtos;
using Shared;

namespace Application.Contracts;
public interface IUserService
{
	Task<Result<List<UserInfoResponse>>> GetAll(UserRequestParameters parameters);
	Task<Result<UserInfoResponse>> GetUserInfo(string id);
	Task<Result<UserInfoResponse>> GetCurrentUserInfo();
	Task<Result<UserInfoResponse>> UpdateUserInfo(string id, UpdateUserInfoRequest request);
	Task<Result<UserInfoResponse>> UpdateCurrentUserInfo(UpdateUserInfoRequest updateRequest);
	Task<Result<UserInfoResponse>> DeleteUser(string userId);
}



