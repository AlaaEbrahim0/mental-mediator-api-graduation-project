using Microsoft.AspNetCore.Mvc;
using Shared;

public static class ResultExtensions
{
	public static IActionResult ToProblemDetails<T>(this Result<T> result)
	{
		if (result.IsSuccess)
		{
			throw new InvalidOperationException();
		}

		var errorType = result.Error.ErrorType;

		var statusCode = GetStatusCode(errorType);
		var title = GetTitle(errorType);

		// Expose the failure under the standard ProblemDetails "detail" member as
		// well as the machine readable "errors" array, so every client can read
		// the reason without having to know the Error type shape.
		var problemDetails = new ProblemDetails
		{
			Status = statusCode,
			Title = title,
			Detail = result.Error.Description,
			Extensions = { { "errors", new[] { result.Error } } }
		};

		return new ObjectResult(problemDetails)
		{
			StatusCode = statusCode
		};
	}

	private static int GetStatusCode(ErrorType errorType)
	{
		return errorType switch
		{
			ErrorType.Validation => StatusCodes.Status400BadRequest,
			ErrorType.NotFound => StatusCodes.Status404NotFound,
			ErrorType.Conflict => StatusCodes.Status409Conflict,
			ErrorType.Forbidden => StatusCodes.Status403Forbidden,
			// The server understood the request but cannot fulfil it right now
			// (for example a dependency such as the mail service is unavailable).
			ErrorType.ServiceUnavailable => StatusCodes.Status503ServiceUnavailable,
			// A failure that is not a validation, conflict or authorization problem is
			// still a client error (e.g. an unconfirmed email). Reporting 500 makes
			// the client treat a recoverable mistake as a server fault.
			ErrorType.Failure => StatusCodes.Status400BadRequest,
			ErrorType
			_ => StatusCodes.Status500InternalServerError
		};
	}

	private static string GetTitle(ErrorType errorType)
	{
		return errorType switch
		{
			ErrorType.Validation => "Bad Request",
			ErrorType.NotFound => "Not Found",
			ErrorType.Conflict => "Conflict",
			ErrorType.Forbidden => "Forbidden",
			ErrorType.ServiceUnavailable => "Service Unavailable",
			ErrorType.Failure => "Bad Request",
			_ => "Server Failure"
		};
	}
}
