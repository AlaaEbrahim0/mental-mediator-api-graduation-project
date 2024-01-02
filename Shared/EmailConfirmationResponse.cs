using System.ComponentModel;

namespace Infrastructure.Services;

public class EmailConfirmationResponse
{
    public string? Message { get; set; }
    public string? Email { get; set; }
}
    