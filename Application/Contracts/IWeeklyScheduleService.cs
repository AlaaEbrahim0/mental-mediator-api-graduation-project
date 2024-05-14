using Application.Dtos.WeeklyScheduleDtos;
using Shared;

namespace Application.Contracts;

public interface IWeeklyScheduleService
{
	Task<Result<DoctorWeeklyScheduleResponse>> GetWeeklySchedule(string doctorId, int scheduleId);
	Task<Result<DoctorWeeklyScheduleResponse>> CreateWeeklySchedule(string doctorId, CreateDoctorWeeklyScheduleRequest request);
	Task<Result<DoctorWeeklyScheduleResponse>> DeleteWeeklySchedule(string doctorId,
		int scheduleId);
	Task<Result<AvailableDayResponse>> GetDay(string doctorId, int scheduleId, int availableDayId);
	Task<Result<AvailableDayResponse>> UpdateDay(string doctorId, int scheduleId, int availableDayId, UpdateAvailableDayRequest request);

	Task<Result<AvailableDayResponse>> AddDay(string doctorId, int scheduleId, CreateAvailableDayRequest request);
	Task<Result<AvailableDayResponse>> DeleteDay(string doctorId, int scheduleId, int availableDayId);
}

