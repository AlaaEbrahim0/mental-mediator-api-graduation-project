using System.Collections.Concurrent;
using Application.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Infrastructure.Caching;
public class InMemoryCacheService : ICacheService
{
	private readonly IDistributedCache _distributedCache;
	private readonly static ConcurrentDictionary<string, bool> CacheKeys = new();

	public InMemoryCacheService(IDistributedCache distributedCache)
	{
		_distributedCache = distributedCache;
	}

	public async Task<T?> GetAsync<T>(string key) where T : class
	{
		var cachedValue = await _distributedCache.GetStringAsync(key);
		if (cachedValue == null)
		{
			return null;
		}

		var value = JsonConvert.DeserializeObject<T>(cachedValue);
		return value;
	}

	public async Task RemoveAsync(string key)
	{
		await _distributedCache.RefreshAsync(key);

		CacheKeys.TryRemove(key, out _);
	}

	public async Task RemoveByAsync(string prefix)
	{
		var tasks = CacheKeys
			.Keys
			.Where(k => k.StartsWith(prefix))
			.Select(k => _distributedCache.RemoveAsync(k));

		await Task.WhenAll(tasks);
	}

	public async Task SetAsync<T>(string key, T value, TimeSpan expirationTime)
	{
		string cachedValue = JsonConvert.SerializeObject(value);

		await _distributedCache.SetStringAsync(key, cachedValue, new DistributedCacheEntryOptions
		{
			AbsoluteExpiration = DateTime.UtcNow + expirationTime
		});

		CacheKeys.TryAdd(key, false);
	}
}
