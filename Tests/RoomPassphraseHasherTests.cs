using FluentAssertions;
using Infrastructure.Utilities;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// The stored value has to fail closed. A room whose hash cannot be parsed must
/// stay shut, because the only alternative is treating an unreadable hash as
/// "no passphrase" and opening a room that its owner deliberately protected.
/// </summary>
public class RoomPassphraseHasherTests
{
    private readonly RoomPassphraseHasher _hasher = new();

    [Fact]
    public void A_hashed_passphrase_verifies_against_itself()
    {
        var hash = _hasher.Hash("s3cret-passphrase");

        _hasher.Verify("s3cret-passphrase", hash).Should().BeTrue();
    }

    [Fact]
    public void The_hash_never_contains_the_passphrase()
    {
        var hash = _hasher.Hash("s3cret-passphrase");

        hash.Should().StartWith("pbkdf2-sha256$");
        hash.Should().NotContain("s3cret-passphrase");
    }

    [Fact]
    public void Two_hashes_of_the_same_passphrase_differ()
    {
        var first = _hasher.Hash("same passphrase");
        var second = _hasher.Hash("same passphrase");

        first.Should().NotBe(second);
        _hasher.Verify("same passphrase", first).Should().BeTrue();
        _hasher.Verify("same passphrase", second).Should().BeTrue();
    }

    [Fact]
    public void A_wrong_passphrase_is_rejected()
    {
        var hash = _hasher.Hash("correct horse");

        _hasher.Verify("correct hors", hash).Should().BeFalse();
        _hasher.Verify("Correct horse", hash).Should().BeFalse();
        _hasher.Verify(string.Empty, hash).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("pbkdf2-sha256$150000$onlythreeparts")]
    [InlineData("pbkdf2-sha256$notanint$c2FsdA==$a2V5")]
    [InlineData("pbkdf2-sha256$0$c2FsdA==$a2V5")]
    [InlineData("pbkdf2-sha256$-5$c2FsdA==$a2V5")]
    [InlineData("bcrypt$150000$c2FsdA==$a2V5")]
    [InlineData("pbkdf2-sha256$150000$!!!$a2V5")]
    [InlineData("pbkdf2-sha256$150000$c2FsdA==$!!!")]
    public void An_unreadable_stored_value_fails_closed(string? storedHash)
    {
        _hasher.Verify("anything", storedHash).Should().BeFalse();
    }

    [Fact]
    public void The_iteration_count_travels_with_the_value()
    {
        // A hash written by an older build with a smaller stretch is still
        // verifiable, which is what makes a future iteration bump a re-hash on
        // the next successful join rather than a migration.
        const int iterations = 1000;
        var salt = new byte[16];
        var key = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            "legacy passphrase",
            salt,
            iterations,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            32);
        var stored = $"pbkdf2-sha256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";

        _hasher.Verify("legacy passphrase", stored).Should().BeTrue();
        _hasher.Verify("different passphrase", stored).Should().BeFalse();
    }

    [Fact]
    public void A_unicode_passphrase_round_trips()
    {
        var passphrase = "حجرة-الطبيب-✓";

        _hasher.Verify(passphrase, _hasher.Hash(passphrase)).Should().BeTrue();
    }
}
