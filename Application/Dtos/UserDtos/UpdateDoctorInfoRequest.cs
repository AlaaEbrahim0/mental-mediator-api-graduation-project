using Domain.Enums;

namespace Application.Dtos.UserDtos;

public class UpdateDoctorInfoRequest : UpdateUserInfoRequest
{
	public string? Biography { get; set; }

	public DoctorSpecialization Specialization { get; set; }
}

