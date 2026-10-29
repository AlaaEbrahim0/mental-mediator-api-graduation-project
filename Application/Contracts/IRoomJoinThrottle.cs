namespace Application.Contracts;

/// <summary>
/// Counts failed passphrase attempts per account and room so a guessed
/// passphrase cannot be tried without limit.
/// </summary>
public interface IRoomJoinThrottle
{
	bool IsBlocked(string userId, int roomId);

	void RegisterFailure(string userId, int roomId);

	void Reset(string userId, int roomId);
}
