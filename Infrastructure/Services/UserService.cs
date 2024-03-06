using Application.Contracts;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.UserDtos;

namespace Infrastructure.Services;
public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IUserClaimsService _userClaimsService;
    private readonly IMapper _mapper;

    public UserService(UserManager<AppUser> userManager, IUserClaimsService userClaimsService, IMapper mapper)
    {
        _userManager = userManager;
        _userClaimsService = userClaimsService;
        _mapper = mapper;
    }

    public async Task<Result<UserInfoResponse>> GetUserInfo(string id)
    {
        var currentUserId = _userClaimsService.GetUserId();
        if (!currentUserId.Equals(id))
        {
            return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
        }
        var user = await _userManager.FindByIdAsync(id);
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
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return UserErrors.NotFound(id);
        }

        _mapper.Map(updateRequest, user);
        await _userManager.UpdateAsync(user);

        var response = _mapper.Map<UserInfoResponse>(user);

        return response;
    }
}
