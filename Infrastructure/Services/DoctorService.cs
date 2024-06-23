using Application.Contracts;
using Application.Dtos.UserDtos;
using AutoMapper;
using Domain.Errors;
using Shared;

namespace Infrastructure.Services;

public class DoctorService : IDoctorService
{
	private readonly IRepositoryManager _repoManager;
	private readonly IUserClaimsService
		_userClaimsService;
	private readonly IStorageService _storageService;
	private readonly IMapper _mapper;
	private readonly IWeeklyScheduleService _weeklyScheduleService;

	public DoctorService(IUserClaimsService userClaimsService, IStorageService storageService, IMapper mapper, IRepositoryManager repoManager, IWeeklyScheduleService weeklyScheduleService)
	{
		_userClaimsService = userClaimsService;
		_storageService = storageService;
		_mapper = mapper;
		_repoManager = repoManager;
		_weeklyScheduleService = weeklyScheduleService;
	}

	public IWeeklyScheduleService WeeklyScheduleService => _weeklyScheduleService;

	public async Task<Result<DoctorInfoResponse>> GetDoctorInfo(string id)
	{
		var user = await _repoManager.Doctors.GetById(id, false);

		if (user is null)
		{
			return UserErrors.NotFound(id);
		}

		var response = _mapper.Map<DoctorInfoResponse>(user);
		return response;

	}
	public async Task<Result<DoctorInfoResponse>> GetCurrentDoctorInfo()
	{
		var currentUserId = _userClaimsService.GetUserId();
		var user = await _repoManager.Doctors.GetById(currentUserId, false);

		if (user is null)
		{
			return UserErrors.NotFound(currentUserId);
		}

		var response = _mapper.Map<DoctorInfoResponse>(user);
		return response;

	}

	public async Task<Result<DoctorInfoResponse>> UpdateDoctorInfo(string doctorId, UpdateDoctorInfoRequest request)
	{
		var user = await _repoManager.Doctors.GetById(doctorId, true);

		if (user is null)
		{
			return UserErrors.NotFound(doctorId);
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
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorInfoResponse>(user);
		return response;
	}
	public async Task<Result<DoctorInfoResponse>> UpdateCurrentDoctorInfo(UpdateDoctorInfoRequest request)
	{
		var currentUserId = _userClaimsService.GetUserId();
		var user = await _repoManager.Doctors.GetById(currentUserId, true);

		if (user is null)
		{
			return UserErrors.NotFound(currentUserId);
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
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorInfoResponse>(user);
		return response;
	}

	public async Task<Result<IEnumerable<DoctorInfoResponse>>> GetAll(DoctorRequestParameters requestParameters)
	{
		var doctors = await _repoManager.Doctors.GetAll(requestParameters, false);
		var response = _mapper.Map<List<DoctorInfoResponse>>(doctors);
		return response;
	}
}
