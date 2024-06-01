using Application.Dtos.UserDtos;
using Shared;

namespace Application.Contracts;

public interface IDoctorService
{
	Task<Result<DoctorInfoResponse>> GetDoctorInfo(string id);
	Task<Result<DoctorInfoResponse>> GetCurrentDoctorInfo();
	Task<Result<DoctorInfoResponse>> UpdateDoctorInfo(string id, UpdateDoctorInfoRequest request);
	Task<Result<DoctorInfoResponse>> UpdateCurrentDoctorInfo(UpdateDoctorInfoRequest request);
	IWeeklyScheduleService WeeklyScheduleService { get; }
}

