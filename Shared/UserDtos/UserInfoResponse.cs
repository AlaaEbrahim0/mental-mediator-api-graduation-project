namespace Shared.UserDtos;
public class UserInfoResponse
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; init; }
    public DateOnly BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? PhotoUrl { get; set; }
}
