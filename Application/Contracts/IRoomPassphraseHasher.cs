namespace Application.Contracts;

/// <summary>
/// Protects room passphrases at rest. Kept separate from ASP.NET Identity's
/// password hashing: a room passphrase is not an account credential and must
/// not change shape when the identity stack is serviced.
/// </summary>
public interface IRoomPassphraseHasher
{
	string Hash(string passphrase);

	bool Verify(string passphrase, string? storedHash);
}
