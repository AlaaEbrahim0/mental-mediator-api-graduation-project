namespace Application.Contracts;
public interface ICacheService
{
	Task<T?> GetAsync<T>(string key)
		where T : class;

	Task SetAsync<T>(string key, T value, TimeSpan expirationTime);

	Task RemoveAsync(string key);

	Task RemoveByAsync(string prefix);
}


