using Application.Dtos.NotificationDtos;

namespace Application.Contracts;
public interface IMailService
{
	Task SendEmailAsync(MailRequest mailRequest);
}
