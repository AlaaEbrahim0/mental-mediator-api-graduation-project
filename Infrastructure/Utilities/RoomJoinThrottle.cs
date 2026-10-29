using Application.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Utilities;

/// <summary>
/// Five wrong passphrases per account and room lock further attempts for a
/// sliding quarter of an hour. In-process like every other limiter here, so a
/// second application instance would multiply the budget.
/// </summary>
public sealed class RoomJoinThrottle : IRoomJoinThrottle
{
	private const int MaxFailures = 5;
	private static readonly TimeSpan BlockWindow = TimeSpan.FromMinutes(15);

	private readonly IMemoryCache _cache;

	public RoomJoinThrottle(IMemoryCache cache)
	{
		_cache = cache;
	}

	public bool IsBlocked(string userId, int roomId) =>
		_cache.TryGetValue(FailureCountKey(userId, roomId), out int failures) && failures >= MaxFailures;

	public void RegisterFailure(string userId, int roomId)
	{
		var key = FailureCountKey(userId, roomId);
		var failures = _cache.TryGetValue(key, out int current) ? current : 0;

		_cache.Set(key, failures + 1, new MemoryCacheEntryOptions { SlidingExpiration = BlockWindow });
	}

	public void Reset(string userId, int roomId) =>
		_cache.Remove(FailureCountKey(userId, roomId));

	private static string FailureCountKey(string userId, int roomId) => $"room-join-failures:{userId}:{roomId}";
}
