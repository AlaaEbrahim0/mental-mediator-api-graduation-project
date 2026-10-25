namespace Application.Dtos.ChatDtos;

/// <summary>
/// Result returned by chat hub methods. Hub methods do not return
/// ProblemDetails, so the client gets the same machine-readable error code and
/// safe description without an HTTP response.
/// </summary>
public class ChatHubResult
{
	public bool Success { get; set; }

	public string? Code { get; set; }

	public string? Message { get; set; }

	public static ChatHubResult Ok() => new() { Success = true };

	public static ChatHubResult Fail(string code, string message) =>
		new() { Success = false, Code = code, Message = message };
}

public class ChatHubResult<T>
{
	public bool Success { get; set; }

	public T? Data { get; set; }

	public string? Code { get; set; }

	public string? Message { get; set; }

	public static ChatHubResult<T> Ok(T data) => new() { Success = true, Data = data };

	public static ChatHubResult<T> Fail(string code, string message) =>
		new() { Success = false, Code = code, Message = message };
}
