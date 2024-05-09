namespace Application.Dtos.AuthDtos;
public class AuthResponse
{
	public string? UserId { get; set; }
	public string? UserName { get; set; }
	public string? Message { get; set; }
	public string? Email { get; set; }
	public string? Token { get; set; }
	public string? PhotoUrl { get; set; }
	public DateTime ExpiresOn { get; set; }
	public List<string> Roles { get; set; } = new();
}
