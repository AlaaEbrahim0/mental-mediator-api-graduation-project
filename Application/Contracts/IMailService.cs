using Application.Dtos.NotificationDtos;
using Shared;

namespace Application.Contracts;
public interface IMailService
{
	/// <summary>
	/// Attempts to deliver an email. Delivery problems are reported through the
	/// returned result instead of an exception, so callers can decide whether a
	/// failed mail is fatal for the operation they are performing.
	/// </summary>
	Task<Result> SendEmailAsync(MailRequest mailRequest);
}
