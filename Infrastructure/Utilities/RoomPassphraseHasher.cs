using System.Security.Cryptography;
using Application.Contracts;

namespace Infrastructure.Utilities;

/// <summary>
/// PBKDF2-SHA256 room passphrases, stored as
/// <c>pbkdf2-sha256$iterations$salt$key</c>. The iteration count travels with
/// the value so a stored hash can be re-stretched on the next successful join
/// instead of in a migration.
/// </summary>
public sealed class RoomPassphraseHasher : IRoomPassphraseHasher
{
	private const string Scheme = "pbkdf2-sha256";
	private const int Iterations = 150_000;
	private const int SaltBytes = 16;
	private const int KeyBytes = 32;

	public string Hash(string passphrase)
	{
		var salt = RandomNumberGenerator.GetBytes(SaltBytes);
		var key = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, Iterations, HashAlgorithmName.SHA256, KeyBytes);

		return string.Join('$', Scheme, Iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(key));
	}

	public bool Verify(string passphrase, string? storedHash)
	{
		if (string.IsNullOrEmpty(storedHash))
		{
			return false;
		}

		var parts = storedHash.Split('$');
		if (parts.Length != 4 ||
			parts[0] != Scheme ||
			!int.TryParse(parts[1], out var iterations) ||
			iterations < 1)
		{
			// Unreadable stored value fails closed. It is never treated as "no
			// passphrase", which would open the room.
			return false;
		}

		try
		{
			var salt = Convert.FromBase64String(parts[2]);
			var expected = Convert.FromBase64String(parts[3]);
			var actual = Rfc2898DeriveBytes.Pbkdf2(passphrase, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

			return CryptographicOperations.FixedTimeEquals(actual, expected);
		}
		catch (FormatException)
		{
			return false;
		}
	}
}
