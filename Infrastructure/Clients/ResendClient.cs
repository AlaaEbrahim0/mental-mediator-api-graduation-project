using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Clients;

/// <summary>
/// Minimal Resend REST client. Uses HttpClient directly so no extra NuGet
/// package is required — the Resend API is a simple JSON POST endpoint at
/// https://api.resend.com/email.
/// </summary>
public interface IResendClient
{
	Task<ResendSendResponse?> SendAsync(
		string from,
		string to,
		string subject,
		string html,
		CancellationToken ct = default);
}

public class ResendClient : IResendClient
{
	private readonly HttpClient _http;
	private readonly ILogger<ResendClient> _logger;

	public ResendClient(
		HttpClient http,
		IOptions<ResendSettings> settings,
		ILogger<ResendClient> logger)
	{
		_http = http;
		_logger = logger;

		// The API key is only sent as a header over HTTPS, never logged.
		_http.DefaultRequestHeaders.Authorization =
			new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.Value.ApiKey!);
		_http.BaseAddress = new Uri("https://api.resend.com/");
		_http.Timeout = TimeSpan.FromSeconds(30);
	}

	public async Task<ResendSendResponse?> SendAsync(
		string from,
		string to,
		string subject,
		string html,
		CancellationToken ct = default)
	{
		var request = new ResendSendRequest
		{
			From = from,
			To = new[] { to },
			Subject = subject,
			Html = html
		};

		try
		{
			var response = await _http.PostAsJsonAsync("email", request, ct);
			if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning(
					"Resend API returned {StatusCode} for email to {Recipient}",
					response.StatusCode, to);
				return null;
			}

			return await response.Content.ReadFromJsonAsync<ResendSendResponse>(cancellationToken: ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			_logger.LogWarning("Resend send to {Recipient} was cancelled", to);
			throw;
		}
		catch (TaskCanceledException)
		{
			_logger.LogWarning("Resend send to {Recipient} timed out", to);
			return null;
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			_logger.LogWarning(ex, "Resend send to {Recipient} failed", to);
			return null;
		}
	}
}

/// <summary>
/// Request body sent to POST https://api.resend.com/email.
/// </summary>
public record ResendSendRequest
{
	[JsonPropertyName("from")]
	public string? From { get; init; }

	[JsonPropertyName("to")]
	public string[]? To { get; init; }

	[JsonPropertyName("subject")]
	public string? Subject { get; init; }

	[JsonPropertyName("html")]
	public string? Html { get; init; }
}

/// <summary>
/// Success response from Resend. A non-null Id means the message was accepted
/// for delivery, not that it reached the inbox.
/// </summary>
public record ResendSendResponse
{
	[JsonPropertyName("id")]
	public string? Id { get; init; }
}
