using Application.Contracts;
using Application.Dtos.WeeklyScheduleDtos;
using AutoMapper;
using Domain.Entities;
using Shared;

namespace Application.Services;

public class WeeklyScheduleService : IWeeklyScheduleService
{
	private readonly IRepositoryManager _repoManager;
	private readonly IUserClaimsService
		_userClaimsService;
	private readonly IMapper _mapper;


	public WeeklyScheduleService(IUserClaimsService userClaimsService, IMapper mapper, IRepositoryManager repoManager)
	{
		_userClaimsService = userClaimsService;
		_mapper = mapper;
		_repoManager = repoManager;
	}


	public async Task<Result<DoctorWeeklyScheduleResponse>> CreateWeeklySchedule(string doctorId, CreateDoctorWeeklyScheduleRequest request)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}

		var doctor = await _repoManager.Doctors.GetById(doctorId, true);
		if (doctor == null)
		{
			return Error.NotFound("Doctors.NotFound", "doctor cannot be found");
		}

		var schedule = await _repoManager.DoctorSchedule.GetSchedule(doctorId, false);

		if (schedule.Any())
		{
			return Error.Conflict("DoctorSchedules.Conflict", "Doctor already has a schedule");
		}

		schedule = _mapper.Map<List<DoctorScheduleWeekDay>>(request);

		await _repoManager.DoctorSchedule.CreateDoctorWeeklySchedule(doctorId, schedule);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorWeeklyScheduleResponse>(schedule);
		return response;

	}

	public async Task<Result<DoctorWeeklyScheduleResponse>> GetWeeklySchedule(string doctorId)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}

		var schedule = await _repoManager.DoctorSchedule.GetSchedule(doctorId, false);

		if (!schedule.Any())
		{
			return Error.NotFound("Doctors.NotFound", "schedule cannot be found");
		}

		var response = _mapper.Map<DoctorWeeklyScheduleResponse>(schedule);
		return response;
	}
}

