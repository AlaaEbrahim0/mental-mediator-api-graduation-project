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
		if (doctor is null)
		{
			return Error.NotFound("Doctors.NotFound", "doctor doesn't exist");
		}

		if (doctor.WeeklySchedule is not null)
		{
			return Error.Conflict("WeeklySchedule.Conflict", "you cannot create a new schedule since the doctor already has a one");
		}

		var weeklySchedule = _mapper.Map<WeeklySchedule>(request);

		weeklySchedule.DoctorId = doctorId;
		_repoManager.WeeklySchedules.CreateWeeklySchedule(weeklySchedule);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorWeeklyScheduleResponse>(weeklySchedule);
		return response;
	}

	public async Task<Result<DoctorWeeklyScheduleResponse>> DeleteWeeklySchedule(string doctorId, int scheduleId)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var schedule = await _repoManager.WeeklySchedules.GetById(doctorId, scheduleId, true);

		if (schedule is null)
		{
			return Error.NotFound("WeeklySchedule.NotFound", "schedule isn't found");
		}

		_repoManager.WeeklySchedules.DeleteWeeklySchecule(schedule);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorWeeklyScheduleResponse>(schedule);
		return response;
	}

	public async Task<Result<AvailableDayResponse>> GetDay(string doctorId, int scheduleId, int availableDayId)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var day = await _repoManager.AvailableDays.GetAvailableDay(scheduleId, availableDayId, false);

		var response = _mapper.Map<AvailableDayResponse>(day);
		return response;
	}

	public async Task<Result<AvailableDayResponse>> DeleteDay(string doctorId, int scheduleId, int availableDayId)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var day = await _repoManager.AvailableDays.GetAvailableDay(scheduleId, availableDayId, true);

		if (day is null)
		{
			return Error.NotFound("AvailableDays.NotFound", "day isn't found");
		}

		_repoManager.AvailableDays.DeleteAvailableDay(day);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<AvailableDayResponse>(day);
		return response;
	}

	public async Task<Result<AvailableDayResponse>> UpdateDay(
		string doctorId,
		int scheduleId,
		int availableDayId,
		UpdateAvailableDayRequest request)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var schedule = await _repoManager.WeeklySchedules.GetById(doctorId, scheduleId, true);

		if (schedule is null)
		{
			return Error.NotFound("WeeklySchedule.NotFound", "schedule isn't found");
		}

		var day = schedule
			.AvailableDays
			.FirstOrDefault(x => x.Id == availableDayId);

		if (day is null)
		{
			return Error.NotFound("WeekDay.NotFound", "weekday isn't found");
		}

		_mapper.Map(request, day);
		_repoManager.WeeklySchedules.UpdateWeeklySchedule(schedule);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<AvailableDayResponse>(day);
		return response;
	}

	public async Task<Result<AvailableDayResponse>> AddDay(
		string doctorId,
		int scheduleId,
		CreateAvailableDayRequest request)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var schedule = await _repoManager.WeeklySchedules.GetById(doctorId, scheduleId, true);

		if (schedule is null)
		{
			return Error.NotFound("WeeklySchedule.NotFound", "schedule isn't found");
		}

		var day = schedule
			.AvailableDays
			.FirstOrDefault(x => x.DayOfWeek == request.DayOfWeek);

		if (day is not null)
		{
			return Error.NotFound("WeekDay.Conflict", "this weekday already exist in the schedule");
		}

		day = _mapper.Map<AvailableDays>(request);
		day.WeeklyScheduleId = schedule.Id;
		//schedule.AvailableDays.Add(day!);
		_repoManager.AvailableDays.CreateAvailableDay(day);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<AvailableDayResponse>(day);
		return response;
	}

	public async Task<Result<DoctorWeeklyScheduleResponse>> GetWeeklySchedule(string doctorId, int scheduleId)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}
		var schedule = await _repoManager.WeeklySchedules.GetById(doctorId, scheduleId, true);

		if (schedule is null)
		{
			return Error.NotFound("WeeklySchedule.NotFound", "schedule isn't found");
		}

		var response = _mapper.Map<DoctorWeeklyScheduleResponse>(schedule);
		return response;
	}
}

