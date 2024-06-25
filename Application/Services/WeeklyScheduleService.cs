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

	public async Task<Result<WeekDayResponse>> AddDay(string doctorId, CreateScheduleWeekDayRequest request)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}

		var weekDay = await _repoManager.DoctorSchedule.GetScheduleWeekDay(doctorId, request.DayOfWeek, true);

		if (weekDay != null)
		{
			return Error.Conflict("DoctorsSchedules.WeekdayConflict", "weekday already exist");
		}

		weekDay = _mapper.Map<DoctorScheduleWeekDay>(request);
		weekDay.DoctorId = doctorId;

		_repoManager.DoctorSchedule.CreateScheduleWeekDay(weekDay);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<WeekDayResponse>(weekDay);
		return response;
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

		if (schedule.WeekDays.Any())
		{
			return Error.Conflict("DoctorSchedules.Conflict", "Doctor already has a schedule");
		}

		schedule = _mapper.Map<WeeklySchedule>(request);
		schedule.WeekDays.ForEach(x => x.DoctorId = doctorId);

		//doctor.SessionFees = request.SessionFees;
		//doctor.Location = request.Location;

		//_repoManager.Doctors.UpdateDoctor(doctor);

		await _repoManager.DoctorSchedule.CreateDoctorWeeklySchedule(doctorId, schedule);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorWeeklyScheduleResponse>(schedule);
		return response;

	}

	public async Task<Result<WeekDayResponse>> DeleteDay(string doctorId, DayOfWeek dayOfWeek)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}

		var weekDay = await _repoManager.DoctorSchedule.GetScheduleWeekDay(doctorId, dayOfWeek, true);

		if (weekDay == null)
		{
			return Error.NotFound("DoctorsDoctorsSchedules.WeekdayNotFound", "weekday cannot be found");
		}

		_repoManager.DoctorSchedule.DeleteScheduleWeekDay(weekDay);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<WeekDayResponse>(weekDay);
		return response;
	}

	public async Task<Result<DoctorWeeklyScheduleResponse>> DeleteWeeklySchedule(string doctorId)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}

		var schedule = await _repoManager.DoctorSchedule.GetSchedule(doctorId, false);

		if (!schedule.WeekDays.Any())
		{
			return Error.NotFound("Doctors.NotFound", "schedule cannot be found");
		}

		_repoManager.DoctorSchedule.DeleteDoctorSchedule(schedule);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorWeeklyScheduleResponse>(schedule);
		return response;
	}

	public async Task<Result<WeekDayResponse>> GetDay(string doctorId, DayOfWeek dayOfWeek)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}

		var weekDay = await _repoManager.DoctorSchedule.GetScheduleWeekDay(doctorId, dayOfWeek, false);

		if (weekDay == null)
		{
			return Error.NotFound("DoctorsDoctorsSchedules.WeekdayNotFound", "weekday cannot be found");
		}

		var response = _mapper.Map<WeekDayResponse>(weekDay);
		return response;
	}

	public Task<Result<DoctorWeeklyScheduleResponse>> GetDoctorWeeklySchedule(string doctorId)
	{
		throw new NotImplementedException();
	}

	public async Task<Result<DoctorWeeklyScheduleResponse>> GetWeeklySchedule(string doctorId)
	{
		var schedule = await _repoManager.DoctorSchedule.GetSchedule(doctorId, false);

		if (!schedule.WeekDays.Any())
		{
			return Error.NotFound("Doctors.NotFound", "schedule cannot be found");
		}

		var response = _mapper.Map<DoctorWeeklyScheduleResponse>(schedule);
		return response;
	}

	public Task<Result<List<DoctorWeeklyScheduleResponse>>> GetWeeklySchedules()
	{
		throw new NotImplementedException();
	}

	public async Task<Result<WeekDayResponse>> UpdateDay(string doctorId, DayOfWeek dayOfWeek, UpdateScheduleWeekDayRequest request)
	{
		var currentUser = _userClaimsService.GetUserId();
		if (currentUser != doctorId)
		{
			return Error.Forbidden("Users.ForbiddenInfo", "you don't have permission to access this resource");
		}

		var weekDay = await _repoManager.DoctorSchedule.GetScheduleWeekDay(doctorId, dayOfWeek, true);

		if (weekDay == null)
		{
			return Error.NotFound("DoctorsDoctorsSchedules.WeekdayNotFound", "weekday cannot be found");
		}

		weekDay = _mapper.Map<DoctorScheduleWeekDay>(request);
		weekDay.DayOfWeek = dayOfWeek;
		weekDay.DoctorId = doctorId;

		_repoManager.DoctorSchedule.UpdateScheduleWeekDay(weekDay);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<WeekDayResponse>(weekDay);
		return response;
	}
}

