using Domain.Enums;

namespace Application.Dtos.UserDtos;
public class BaseUserInfoResponse
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; init; } = string.Empty;
	public DateOnly BirthDate { get; set; }
	public string Gender { get; set; } = string.Empty;
	public string? PhotoUrl { get; set; }
}


public class UserInfoResponse : BaseUserInfoResponse { }

public class DoctorInfoResponse : BaseUserInfoResponse
{
	public string? Biography { get; set; }

	public DoctorSpecialization Specialization { get; set; }
}
