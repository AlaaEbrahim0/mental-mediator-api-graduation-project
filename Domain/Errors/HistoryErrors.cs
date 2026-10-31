using Shared;

namespace Domain.Errors;

public static class HistoryErrors
{
	public static Error NotFound() => Error.NotFound(
		"History.NotFound",
		"The requested patient history is not available to this account.");
}
