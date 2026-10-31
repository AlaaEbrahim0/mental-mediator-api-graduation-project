using Application.Contracts;
using Application.Dtos.NotificationDtos;
using Application.Options;
using Domain.Errors;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Shared;

namespace Infrastructure.Services;

public class MailService : IMailService
{
    private readonly MailSettings _mailSettings;
    private readonly ILogger<MailService> _logger;

    public MailService(IOptions<MailSettings> mailOptions, ILogger<MailService> logger)
    {
        _mailSettings = mailOptions.Value;
        _logger = logger;
    }

    public async Task<Result> SendEmailAsync(MailRequest mailRequest)
    {
        // The offending setting name is logged but never returned: the whole
        // Error object is serialized into ProblemDetails, so a client would
        // otherwise learn which server configuration is missing.
        var missingSetting = FindMissingSetting();
        if (missingSetting is not null)
        {
            _logger.LogWarning(
                "Mail is not configured (missing {Setting}), skipped sending \"{Subject}\"",
                missingSetting,
                mailRequest.Subject);

            return MailErrors.NotConfigured();
        }

        MimeMessage message;
        try
        {
            message = BuildMessage(mailRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build email \"{Subject}\"", mailRequest.Subject);

            return MailErrors.MessageBuildFailed(mailRequest.ToEmail ?? "the recipient");
        }

        try
        {
            using var smtp = new SmtpClient();

            // Trimmed at use: app passwords pasted from the Google dialog arrive
            // with surrounding whitespace, which the provider rejects.
            // SecureSocketOptions.Auto picks implicit TLS on 465 and START TLS on
            // 587/25, so switching providers or ports needs no code change.
            await smtp.ConnectAsync(_mailSettings.Host.Trim(), _mailSettings.Port, SecureSocketOptions.Auto);
            await smtp.AuthenticateAsync(_mailSettings.Username.Trim(), _mailSettings.Password.Trim());
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // The credential, host, and port values are never included: this
            // message ends up in application logs on shared hosting.
            _logger.LogError(
                ex,
                "SMTP delivery failed for \"{Subject}\" via {Host}:{Port}",
                mailRequest.Subject,
                _mailSettings.Host,
                _mailSettings.Port);

            return MailErrors.DeliveryFailed(mailRequest.ToEmail ?? "the recipient");
        }

        // SendAsync returning without an exception means the SMTP server accepted
        // the message for delivery; it is not proof of inbox delivery.
        _logger.LogInformation("SMTP provider accepted \"{Subject}\" for {Recipient}",
            mailRequest.Subject, mailRequest.ToEmail);

        return Result.Success();
    }

    /// <summary>
    /// A message with only a Sender and no From violates RFC 5322 and Gmail
    /// rejects it, so the sender address is mirrored into From with an optional
    /// display name.
    /// </summary>
    private MimeMessage BuildMessage(MailRequest mailRequest)
    {
        var sender = MailboxAddress.Parse(_mailSettings.Username);

        var email = new MimeMessage();
        email.From.Add(string.IsNullOrWhiteSpace(_mailSettings.SenderName)
            ? sender
            : new MailboxAddress(_mailSettings.SenderName, sender.Address));
        email.Sender = sender;
        email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
        email.Subject = mailRequest.Subject;

        var body = new BodyBuilder { HtmlBody = mailRequest.Body };
        if (!string.IsNullOrWhiteSpace(mailRequest.TextBody))
        {
            body.TextBody = mailRequest.TextBody;
        }
        email.Body = body.ToMessageBody();

        return email;
    }

    private string? FindMissingSetting()
    {
        if (string.IsNullOrWhiteSpace(_mailSettings.Host))
        {
            return "MailSettings:Host";
        }

        if (_mailSettings.Port is <= 0 or > 65535)
        {
            return "MailSettings:Port";
        }

        if (string.IsNullOrWhiteSpace(_mailSettings.Username))
        {
            return "MailSettings:Username";
        }

        if (string.IsNullOrWhiteSpace(_mailSettings.Password))
        {
            return "MailSettings:Password";
        }

        return null;
    }
}
