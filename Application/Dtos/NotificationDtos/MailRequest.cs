using System.ComponentModel.DataAnnotations;

namespace Application.Dtos.NotificationDtos;
public class MailRequest
{
	[Required]
	[EmailAddress(ErrorMessage = "Invalid email address format")]
	public string? ToEmail { get; set; }

	[Required]
	public string? Subject { get; set; }

	[Required]
	public string? Body { get; set; }
}