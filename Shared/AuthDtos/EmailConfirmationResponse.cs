using System.ComponentModel;

namespace Shared.AuthDtos;

public class EmailConfirmationResponse
{
    public string? Message { get; set; }
    public string? Email { get; set; }
}
