using Application.Contracts;
using Application.Dtos.UserDtos;
using AutoMapper;
using Domain.Errors;
using Shared;

namespace Infrastructure.Services;
public class UserService : IUserService
{
	private readonly IRepositoryManager _repos;
	private readonly IUserClaimsService _userClaimsService;
	private readonly IStorageService _storageService;
	private readonly IMapper _mapper;

	public UserService(IUserClaimsService userClaimsService, IMapper mapper, IStorageService storageService, IRepositoryManager repos)
	{
		_userClaimsService = userClaimsService;
		_mapper = mapper;
		_storageService = storageService;
		_repos = repos;
	}

	public async Task<Result<UserInfoResponse>> GetUserInfo(string id)
	{
		var currentUserId = _userClaimsService.GetUserId();
		if (!currentUserId.Equals(id))
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var user = await _repos.Users.GetById(id, false);
		var response = _mapper.Map<UserInfoResponse>(user);

		return response;

	}

	public async Task<Result<UserInfoResponse>> UpdateUserInfo(string id, UpdateUserInfoRequest updateRequest)
	{
		var currentUserId = _userClaimsService.GetUserId();
		if (!currentUserId.Equals(id))
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var user = await _repos.Users.GetById(id, true);

		if (user is null)
		{
			return UserErrors.NotFound(id);
		}

		if (updateRequest.Photo is not null)
		{
			var uploadResult = await _storageService.UploadPhoto(updateRequest.Photo);
			if (uploadResult.IsFailure)
			{
				return uploadResult.Error;
			}
			user.PhotoUrl = uploadResult.Value;
		}

		_mapper.Map(updateRequest, user);
		_repos.Users.UpdateUserInfo(user);
		await _repos.SaveAsync();

		var response = _mapper.Map<UserInfoResponse>(user);
		return response;

	}
}
