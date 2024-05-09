using Application.Dtos.UserDtos;
using Shared;
using Shared.UserDtos;

namespace Application.Contracts;

public interface IDoctorService
{
	Task<Result<DoctorInfoResponse>> GetDoctorInfo(string id);
	Task<Result<DoctorInfoResponse>> UpdateDoctorInfo(string id, UpdateDoctorInfoRequest request);
}

