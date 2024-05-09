using Application.Contracts;
using Application.Dtos.UserDtos;
using AutoMapper;
using Domain.Errors;
using Shared;
using Shared.UserDtos;

namespace Infrastructure.Services;

public class DoctorService : IDoctorService
{
	private readonly IRepositoryManager _repoManager;
	private readonly IUserClaimsService
		_userClaimsService;
	private readonly IStorageService _storageService;
	private readonly IMapper _mapper;

	public DoctorService(IUserClaimsService userClaimsService, IStorageService storageService, IMapper mapper, IRepositoryManager repoManager)
	{
		_userClaimsService = userClaimsService;
		_storageService = storageService;
		_mapper = mapper;
		_repoManager = repoManager;
	}

	public async Task<Result<DoctorInfoResponse>> GetDoctorInfo(string id)
	{
		var currentUserId = _userClaimsService.GetUserId();
		if (!currentUserId.Equals(id))
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var user = await _repoManager.Doctors.GetById(id, false);

		if (user is null)
		{
			return UserErrors.NotFound(id);
		}

		var response = _mapper.Map<DoctorInfoResponse>(user);

		return response;
	}

	public async Task<Result<DoctorInfoResponse>> UpdateDoctorInfo(string id, UpdateDoctorInfoRequest request)
	{
		var currentUserId = _userClaimsService.GetUserId();
		if (!currentUserId.Equals(id))
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var user = await _repoManager.Doctors.GetById(id, true);

		if (user is null)
		{
			return UserErrors.NotFound(id);
		}

		if (request.Photo is not null)
		{
			var uploadResult = await _storageService.UploadPhoto(request.Photo);
			if (uploadResult.IsFailure)
			{
				return uploadResult.Error;
			}
			user.PhotoUrl = uploadResult.Value;
		}

		_mapper.Map(request, user);
		_repoManager.Doctors.UpdateDoctor(user);

		var response = _mapper.Map<DoctorInfoResponse>(user);
		return response;
	}
}
