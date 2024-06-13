using System.Collections.Concurrent;
using Application.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Caching
{
	public class InMemoryCacheService : ICacheService
	{
		private readonly IMemoryCache _memoryCache;
		private readonly ConcurrentDictionary<string, bool> _cacheKeys = new();

		public InMemoryCacheService(IMemoryCache memoryCache)
		{
			_memoryCache = memoryCache;
		}

		public Task<T?> GetAsync<T>(string key) where T : class
		{
			_memoryCache.TryGetValue(key, out T? value);
			return Task.FromResult(value);
		}

		public Task RemoveAsync(string key)
		{
			_memoryCache.Remove(key);
			_cacheKeys.TryRemove(key, out _);
			return Task.CompletedTask;
		}

		public Task SetAsync<T>(string key, T value, TimeSpan expirationTime)
		{
			var cacheEntryOptions = new MemoryCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = expirationTime
			};

			_memoryCache.Set(key, value, cacheEntryOptions);
			_cacheKeys[key] = true;
			return Task.CompletedTask;
		}

		public Task RemoveByAsync(string prefix)
		{
			var keysToRemove = _cacheKeys.Keys.Where(key => key.StartsWith(prefix)).ToList();
			foreach (var key in keysToRemove)
			{
				_memoryCache.Remove(key);
				_cacheKeys.TryRemove(key, out _);
			}
			return Task.CompletedTask;
		}

		public Task InvalidateAllCacheAsync()
		{
			foreach (var key in _cacheKeys.Keys.ToList())
			{
				_memoryCache.Remove(key);
				_cacheKeys.TryRemove(key, out _);
			}
			return Task.CompletedTask;
		}
	}
}
