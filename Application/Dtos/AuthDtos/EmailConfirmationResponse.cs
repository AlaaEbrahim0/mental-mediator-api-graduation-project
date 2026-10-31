namespace Application.Dtos.AuthDtos;

public class EmailConfirmationResponse
{
	public string? Message { get; set; }
	public string? Email { get; set; }

	/// <summary>
	/// Portal URL to continue to after a successful confirmation. Chosen on
	/// the server from the account role and configured portal origin; it is
	/// never accepted from the client.
	/// </summary>
	public string? RedirectUrl { get; set; }
}
