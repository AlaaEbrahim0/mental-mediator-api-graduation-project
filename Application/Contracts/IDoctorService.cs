using Application.Dtos;
using Application.Dtos.UserDtos;
using Shared;

namespace Application.Contracts;

public interface IDoctorService
{
	Task<Result<IEnumerable<DoctorInfoResponse>>> GetAll(DoctorRequestParameters requestParameters);
	Task<Result<DoctorInfoResponse>> GetDoctorInfo(string id);
	Task<Result<DoctorInfoResponse>> GetCurrentDoctorInfo();
	Task<Result<DoctorInfoResponse>> UpdateDoctorInfo(string id, UpdateDoctorInfoRequest request);
	Task<Result<DoctorInfoResponse>> UpdateCurrentDoctorInfo(UpdateDoctorInfoRequest request);
	IWeeklyScheduleService WeeklyScheduleService { get; }

	Task<Result<DoctorInfoResponse>> DeleteDoctor(string doctorId);
	Task<Result<List<TimeSpan>>> GetAvailableSlots(string doctorId, DateTime date);
	Task<Result<DoctorReportResponse>> GetReports();
}

