using System.Security.Claims;
using FluentAssertions;
using Infrastructure.Utilities;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// The Google/OIDC principal is read by claim name, and the names differ
/// depending on whether the token handler applied its legacy inbound map.
/// These tests pin the reader against the shapes the external cookie can
/// actually arrive in: a missing email must produce null (the old code threw a
/// NullReferenceException inside the OAuth callback), and an unverifiable
/// address must never be reported as verified.
/// </summary>
public class ExternalLoginProfileTests
{
    private const string LongEmailVerified =
        "http://schemas.xmlsoap.org/ws/2005/09/identity/claims/emailverified";

    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims) =>
        new(new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Type, claim.Value)),
            authenticationType: "TestOidc",
            nameType: ClaimTypes.Name,
            roleType: "roles"));

    private static ExternalLoginProfile Read(params (string Type, string Value)[] claims) =>
        ExternalLoginProfileReader.TryRead(Principal(claims))!;

    [Fact]
    public void Reads_the_google_claim_spelling()
    {
        var profile = Read(
            ("email", "Ali@Example.com"),
            ("given_name", "Ali"),
            ("family_name", "Hassan"),
            ("picture", "https://lh3.googleusercontent.com/a/photo"),
            ("email_verified", "true"));

        profile.Email.Should().Be("Ali@Example.com");
        profile.FirstName.Should().Be("Ali");
        profile.LastName.Should().Be("Hassan");
        profile.PhotoUrl.Should().Be("https://lh3.googleusercontent.com/a/photo");
        profile.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public void Reads_the_mapped_claim_uris_when_the_handler_mapped_inbound_claims()
    {
        var profile = Read(
            (ClaimTypes.Email, "ali@example.com"),
            (ClaimTypes.GivenName, "Ali"),
            (ClaimTypes.Surname, "Hassan"),
            (LongEmailVerified, "true"));

        profile.Email.Should().Be("ali@example.com");
        profile.FirstName.Should().Be("Ali");
        profile.LastName.Should().Be("Hassan");
        profile.EmailVerified.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_principal_without_a_usable_email_reads_as_no_profile(string? email)
    {
        var claims = new List<(string, string)> { ("given_name", "Ali") };
        if (email is not null)
        {
            claims.Add(("email", email));
        }

        ExternalLoginProfileReader
            .TryRead(Principal(claims.ToArray()))
            .Should().BeNull("the account cannot be identified without an address");
    }

    [Fact]
    public void A_missing_principal_reads_as_no_profile() =>
        ExternalLoginProfileReader.TryRead(null).Should().BeNull();

    [Fact]
    public void Email_is_trimmed_but_not_lowercased()
    {
        var profile = Read(("email", "  Ali.Hassan@Example.com  "), ("email_verified", "true"));

        profile.Email.Should().Be("Ali.Hassan@Example.com");
    }

    [Theory]
    [InlineData("Ali", "Ali", null)]
    [InlineData("Ali Hassan", "Ali", "Hassan")]
    [InlineData("Ali Hassan Pasha", "Ali", "Hassan Pasha")]
    [InlineData("   Ali    Hassan  ", "Ali", "Hassan")]
    public void A_display_name_splits_into_given_and_family_name(
        string name,
        string expectedFirst,
        string? expectedLast)
    {
        var profile = Read(("email", "ali@example.com"), ("name", name));

        profile.FirstName.Should().Be(expectedFirst);
        profile.LastName.Should().Be(expectedLast);
    }

    [Fact]
    public void Given_and_family_name_win_over_the_display_name()
    {
        var profile = Read(
            ("email", "ali@example.com"),
            ("name", "Wrong Person"),
            ("given_name", "Ali"),
            ("family_name", "Hassan"));

        profile.FirstName.Should().Be("Ali");
        profile.LastName.Should().Be("Hassan");
    }

    [Fact]
    public void FirstName_falls_back_to_the_email_local_part_because_the_column_is_required()
    {
        var profile = Read(("email", "ali@example.com"), ("email_verified", "true"));

        profile.FirstName.Should().Be("ali");
        profile.LastName.Should().BeNull();
    }

    [Fact]
    public void An_empty_photo_url_is_not_kept_as_one()
    {
        var profile = Read(("email", "ali@example.com"), ("picture", "  "));

        profile.PhotoUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("TRUE", true)]
    [InlineData("maybe", false)]
    [InlineData("", false)]
    public void Email_verified_is_only_trusted_when_the_claim_says_so(string value, bool expected)
    {
        var profile = Read(("email", "ali@example.com"), ("email_verified", value));

        profile.EmailVerified.Should().Be(expected);
    }

    [Fact]
    public void A_missing_email_verified_claim_is_treated_as_unverified()
    {
        var profile = Read(("email", "ali@example.com"), ("given_name", "Ali"));

        profile.EmailVerified.Should().BeFalse("account creation trusts Google's assertion");
    }

    [Fact]
    public void The_mapped_verified_claim_is_read_under_its_long_name()
    {
        Read(("email", "ali@example.com"), (LongEmailVerified, "true"))
            .EmailVerified.Should().BeTrue();

        Read(("email", "ali@example.com"), (LongEmailVerified, "false"))
            .EmailVerified.Should().BeFalse();
    }

    [Fact]
    public void Claim_types_are_described_for_the_log_line_without_leaking_values()
    {
        var principal = Principal(
            ("email", "ali@example.com"),
            ("given_name", "Ali"),
            ("email_verified", "true"));

        var described = ExternalLoginProfileReader.DescribeClaims(principal);

        described.Should().Be("email, email_verified, given_name");
        described.Should().NotContain("ali@example.com");
    }

    [Fact]
    public void A_missing_principal_is_described_rather_than_throwing() =>
        ExternalLoginProfileReader.DescribeClaims(null).Should().Be("(no principal)");
}
