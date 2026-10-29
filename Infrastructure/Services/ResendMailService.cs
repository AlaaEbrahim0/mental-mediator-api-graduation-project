using Application.Contracts;
using Application.Dtos.NotificationDtos;
using Application.Options;
using Domain.Errors;
using Infrastructure.Clients;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace Infrastructure.Services;

/// <summary>
/// Mail service that prefers Resend for transactional email delivery.
/// When Resend is not configured (no API key or sender), it transparently
/// falls back to the MailKit SMTP implementation so existing SMTP deployments
/// keep working during the transition period.
/// </summary>
public class ResendMailService : IMailService
{
	private readonly IMailService _fallback;
	private readonly IResendClient _resendClient;
	private readonly ResendSettings _settings;
	private readonly ILogger<ResendMailService> _logger;

	public ResendMailService(
		IResendClient resendClient,
		IOptions<ResendSettings> settings,
		IMailService fallback,
		ILogger<ResendMailService> logger)
	{
		_resendClient = resendClient;
		_settings = settings.Value;
		_fallback = fallback;
		_logger = logger;
	}

	public async Task<Result> SendEmailAsync(MailRequest mailRequest)
	{
		if (!_settings.IsConfigured)
		{
			// Resend is not configured — fall back to MailKit SMTP.
			_logger.LogDebug(
				"Resend is not configured; falling back to SMTP for \"{Subject}\"",
				mailRequest.Subject);
			return await _fallback.SendEmailAsync(mailRequest);
		}

		// Build the "From" header: "Display Name <email@domain.com>".
		// Resend requires the From address to use a verified domain.
		var from = string.IsNullOrWhiteSpace(_settings.FromName)
			? _settings.FromEmail
			: $"{_settings.FromName} <{_settings.FromEmail}>";

		string? responseId;
		try
		{
			var response = await _resendClient.SendAsync(
				from!,
				mailRequest.ToEmail!,
				mailRequest.Subject!,
				mailRequest.Body!,
				CancellationToken.None);
			responseId = response?.Id;
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			_logger.LogWarning(
				ex,
				"Resend client threw for \"{Subject}\" to {Recipient}",
				mailRequest.Subject,
				mailRequest.ToEmail);
			return MailErrors.DeliveryFailed(mailRequest.ToEmail ?? "the recipient");
		}

		if (string.IsNullOrEmpty(responseId))
		{
			// Resend rejected or did not accept the message. The caller
			// (e.g. AuthService.SendEmailConfirmationLink) logs the failure
			// and reports it. We do not fall back to SMTP here because the
			// configuration explicitly opted into Resend — a silent fallback
			// could mask a misconfiguration during a migration.
			_logger.LogWarning(
				"Resend did not accept email \"{Subject}\" to {Recipient}; no message ID returned",
				mailRequest.Subject,
				mailRequest.ToEmail);

			return MailErrors.DeliveryFailed(mailRequest.ToEmail ?? "the recipient");
		}

		// A successful API response means Resend accepted the message for
		// delivery — it is not proof of inbox placement.
		_logger.LogInformation(
			"Resend accepted \"{Subject}\" for {Recipient} (message id: {MessageId})",
			mailRequest.Subject,
			mailRequest.ToEmail,
			responseId);

		return Result.Success();
	}
}
