namespace Shared;
public class AuthResponse
{
    public string? Message { get; set; }
    public string? Email { get; set; } 
    public string? Token { get; set; } 
    public bool IsAuthenticated { get; set; }
    public DateTime ExpiresOn { get; set; }
    public List<string> Roles { get; set; } = new();
}
