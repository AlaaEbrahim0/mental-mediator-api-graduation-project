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
	/// <param name="cancellationToken">
	/// Propagates the caller's cancellation (e.g. an aborted request) to the
	/// provider call. Defaults to none so existing callers are unaffected.
	/// </param>
	Task<Result> SendEmailAsync(MailRequest mailRequest, CancellationToken cancellationToken = default);
}
