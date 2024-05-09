namespace Shared.UserDtos;
public class UserInfoResponse
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string Email { get; init; } = string.Empty;
	public DateOnly BirthDate { get; set; }
	public string Gender { get; set; } = string.Empty;
	public string? PhotoUrl { get; set; }
}

public class ClientInfoResponse : UserInfoResponse
{

}

public class DoctorInfoResponse : UserInfoResponse
{
	public string? Biography { get; set; }
	public string Specialization { get; set; } = string.Empty;

}
