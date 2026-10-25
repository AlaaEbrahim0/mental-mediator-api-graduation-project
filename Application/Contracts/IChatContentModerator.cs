using Shared;

namespace Application.Contracts;

public interface IChatContentModerator
{
	Task<Result> EnsureAllowedAsync(string content);
}
