using Application.Dtos.WeeklyScheduleDtos;
using Shared;

namespace Application.Contracts;

public interface IWeeklyScheduleService
{
	Task<Result<DoctorWeeklyScheduleResponse>> GetWeeklySchedule(string doctorId);
	Task<Result<DoctorWeeklyScheduleResponse>> CreateWeeklySchedule(string doctorId, CreateDoctorWeeklyScheduleRequest request);
	//Task<Result<DoctorWeeklyScheduleResponse>> DeleteWeeklySchedule(string doctorId);
	//Task<Result<WeekDayResponse>> GetDay(string doctorId);
	//Task<Result<WeekDayResponse>> UpdateDay(string doctorId, DayOfWeek dayOfWeek, UpdateScheduleWeekDayRequest request);

	//Task<Result<WeekDayResponse>> AddDay(string doctorId, CreateScheduleWeekDayRequest request);
	//Task<Result<WeekDayResponse>> DeleteDay(string doctorId, DayOfWeek dayOfWeek);
}

