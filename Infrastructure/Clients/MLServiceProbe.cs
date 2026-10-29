using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Clients;

public enum MLProbeStatus
{
	/// <summary>The model answered and the JSON was readable.</summary>
	Completed,

	/// <summary>No answer inside the budget, which includes a sleeping service still starting up.</summary>
	TimedOut,

	/// <summary>Unreachable, rate limited, or answered with a server error.</summary>
	Unavailable,

	/// <summary>Answered, but the body was missing or unreadable.</summary>
	InvalidResponse,
}

public sealed record MLProbeResult<T>(MLProbeStatus Status, T? Value);

/// <summary>
/// Transport for the hosted prediction service, shared by every caller so the
/// cold-start budget is decided in one place.
///
/// The service is hosted on Render and sleeps when idle, so its first request
/// after a quiet period can take tens of seconds. One budget therefore covers a
/// whole attempt sequence rather than a single request, and a timed-out attempt
/// is never retried because there is no time left to spend.
/// </summary>
public sealed class MLServiceProbe
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<MLServiceProbe> _logger;
	private readonly TimeSpan _budget;
	private readonly int _maxRetries;
	private readonly TimeSpan _backoff;

	public MLServiceProbe(
		IHttpClientFactory httpClientFactory,
		IConfiguration configuration,
		ILogger<MLServiceProbe> logger)
	{
		_httpClientFactory = httpClientFactory;
		_logger = logger;
		_budget = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("MLServer:TimeoutSeconds", 45)));
		_maxRetries = Math.Max(0, configuration.GetValue("MLServer:MaxRetries", 2));
		_backoff = TimeSpan.FromSeconds(Math.Max(0, configuration.GetValue("MLServer:RetryBackoffSeconds", 1)));
	}

	public async Task<MLProbeResult<T>> PredictAsync<T>(string httpClientName, string path, object payload)
	{
		var deadline = DateTime.UtcNow + _budget;

		for (var attempt = 0; ; attempt++)
		{
			var remaining = deadline - DateTime.UtcNow;
			if (remaining <= TimeSpan.Zero)
			{
				return new MLProbeResult<T>(MLProbeStatus.TimedOut, default);
			}

			try
			{
				var client = _httpClientFactory.CreateClient(httpClientName);
				using var timeoutSource = new CancellationTokenSource(remaining);

				using var response = await client.PostAsJsonAsync(path, payload, timeoutSource.Token);

				// A gateway error is what Render returns while the container is
				// still coming up, so it fails fast and is worth another attempt.
				if (IsTransientStatus(response.StatusCode))
				{
					if (await TryScheduleRetryAsync(attempt, response.StatusCode, deadline, timeoutSource.Token))
					{
						continue;
					}

					return new MLProbeResult<T>(MLProbeStatus.Unavailable, default);
				}

				if (!response.IsSuccessStatusCode)
				{
					_logger.LogWarning(
						"ML request to {Path} failed with status {StatusCode}.",
						path,
						(int)response.StatusCode);
					return new MLProbeResult<T>(MLProbeStatus.Unavailable, default);
				}

				var value = await response.Content.ReadFromJsonAsync<T>(timeoutSource.Token);
				return value is null
					? new MLProbeResult<T>(MLProbeStatus.InvalidResponse, default)
					: new MLProbeResult<T>(MLProbeStatus.Completed, value);
			}
			catch (OperationCanceledException)
			{
				// Nothing is passed in from a caller, so cancellation here can
				// only be this budget expiring. It is never read as a decision.
				_logger.LogWarning("ML request to {Path} did not answer within {BudgetSeconds}s.", path, _budget.TotalSeconds);
				return new MLProbeResult<T>(MLProbeStatus.TimedOut, default);
			}
			catch (HttpRequestException exception)
			{
				if (await TryScheduleRetryAsync(attempt, null, deadline, CancellationToken.None))
				{
					continue;
				}

				_logger.LogWarning(
					exception,
					"ML request to {Path} could not be delivered: {Reason}",
					path,
					exception.GetType().Name);
				return new MLProbeResult<T>(MLProbeStatus.Unavailable, default);
			}
			catch (JsonException exception)
			{
				_logger.LogWarning(
					exception,
					"ML response from {Path} could not be read: {Reason}",
					path,
					exception.GetType().Name);
				return new MLProbeResult<T>(MLProbeStatus.InvalidResponse, default);
			}
			catch (Exception exception) when (IsTransportFault(exception))
			{
				_logger.LogWarning(
					exception,
					"ML request to {Path} failed: {Reason}",
					path,
					exception.GetType().Name);
				return new MLProbeResult<T>(MLProbeStatus.Unavailable, default);
			}
		}
	}

	/// <summary>
	/// Waits out the backoff when the attempt budget and the deadline both
	/// allow it, so a retry can never push past the cold-start window.
	/// </summary>
	private async Task<bool> TryScheduleRetryAsync(
		int attempt,
		HttpStatusCode? statusCode,
		DateTime deadline,
		CancellationToken cancellationToken)
	{
		if (attempt >= _maxRetries)
		{
			return false;
		}

		var backoff = TimeSpan.FromTicks(_backoff.Ticks * (1L << attempt));
		if (DateTime.UtcNow + backoff >= deadline)
		{
			return false;
		}

		_logger.LogWarning(
			"Retrying ML request in {BackoffSeconds}s (attempt {Attempt} of {MaxAttempts}, status {StatusCode}).",
			backoff.TotalSeconds,
			attempt + 1,
			_maxRetries + 1,
			statusCode is null ? "transport-error" : ((int)statusCode).ToString());

		await Task.Delay(backoff, cancellationToken);
		return true;
	}

	private static bool IsTransientStatus(HttpStatusCode statusCode) =>
		statusCode is HttpStatusCode.RequestTimeout
			or HttpStatusCode.TooManyRequests
			or HttpStatusCode.InternalServerError
			or HttpStatusCode.BadGateway
			or HttpStatusCode.ServiceUnavailable
			or HttpStatusCode.GatewayTimeout;

	/// <summary>
	/// Failures the HTTP stack can raise for a service that is asleep or
	/// unreachable. Anything else is a defect and must not be swallowed.
	/// </summary>
	private static bool IsTransportFault(Exception exception) =>
		exception is NotSupportedException or IOException or InvalidOperationException or UriFormatException;
}
