namespace Application.Options;
public class MailSettings
{
    public string? Host { get; set; }
    public int Port { get; set; }

    /// <summary>Authenticated SMTP account. Also used as the From address.</summary>
    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>
    /// Optional display name shown beside the From address. When empty the
    /// address is used on its own. Not a secret; safe for appsettings.
    /// </summary>
    public string? SenderName { get; set; }
}
