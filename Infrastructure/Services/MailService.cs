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
        if (string.IsNullOrWhiteSpace(_mailSettings.Username))
        {
            _logger.LogWarning(
                "Mail is not configured, skipped sending \"{Subject}\" to {Recipient}",
                mailRequest.Subject,
                mailRequest.ToEmail);

            return MailErrors.NotConfigured();
        }

        try
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_mailSettings.Username);
            email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
            email.Subject = mailRequest.Subject;

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = mailRequest.Body;
            email.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_mailSettings.Username, _mailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);

            _logger.LogInformation("Email sent to {Recipient} with subject: {Subject}", mailRequest.ToEmail, mailRequest.Subject);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", mailRequest.ToEmail);

            return MailErrors.DeliveryFailed(mailRequest.ToEmail ?? "the recipient");
        }
    }
}
