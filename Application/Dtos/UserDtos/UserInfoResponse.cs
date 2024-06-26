﻿using Domain.Enums;

namespace Application.Dtos.UserDtos;
public class BaseUserInfoResponse
{
	public string Id { get; set; } = string.Empty;
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
	public string Biography { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string Location { get; set; } = string.Empty;
	public decimal SessionFees { get; set; }
	public DoctorSpecialization Specialization { get; set; }

}
