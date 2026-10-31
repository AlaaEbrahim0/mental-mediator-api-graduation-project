using FluentAssertions;
using Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Two rules are pinned here. Which portal a handoff returns to is derived from
/// the account's roles and never from a caller-supplied destination, and the
/// only thing that reaches a browser URL is a code from a fixed allow-list —
/// failure descriptions in this flow can contain an email address, and a
/// top-level navigation writes its query string into browser history.
/// </summary>
public class AuthHandoffTests
{
    private static IConfiguration Config(string patient, string doctor) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AuthHandoff.PatientSetting] = patient,
            [AuthHandoff.DoctorSetting] = doctor,
        }).Build();

    private static HttpRequest Request() => new DefaultHttpContext
    {
        Request = { Scheme = "https", Host = new HostString("nexus-api.runasp.net") }
    }.Request;

    [Theory]
    [InlineData(new[] { "User" }, AuthHandoff.PatientSetting)]
    [InlineData(new[] { "user" }, AuthHandoff.PatientSetting)]
    [InlineData(new[] { "Doctor" }, AuthHandoff.DoctorSetting)]
    [InlineData(new[] { "doctor" }, AuthHandoff.DoctorSetting)]
    [InlineData(new[] { "Admin" }, AuthHandoff.PatientSetting)]
    [InlineData(new string[0], AuthHandoff.PatientSetting)]
    [InlineData(new[] { "User", "Doctor" }, AuthHandoff.DoctorSetting)]
    public void The_portal_follows_the_account_roles(string[] roles, string expectedSetting) =>
        AuthHandoff.SettingForRoles(roles).Should().Be(expectedSetting);

    [Fact]
    public void A_configured_portal_is_used_and_its_trailing_slash_dropped()
    {
        var url = AuthHandoff.ResolveBaseUrl(
            Config("https://patient.example.com/", "https://doctor.example.com/"),
            Request(),
            AuthHandoff.PatientSetting);

        url.Should().Be("https://patient.example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_unconfigured_portal_falls_back_to_the_request_origin(string configured)
    {
        var url = AuthHandoff.ResolveBaseUrl(
            Config(configured, configured), Request(), AuthHandoff.PatientSetting);

        url.Should().Be("https://nexus-api.runasp.net");
    }

    [Fact]
    public void A_confirmed_email_redirect_only_uses_a_validated_configured_origin()
    {
        var url = AuthHandoff.TryResolveConfiguredBaseUrl(
            Config("https://patient.example.com/", "https://doctor.example.com"),
            AuthHandoff.PatientSetting);

        url.Should().Be("https://patient.example.com");
    }

    [Theory]
    [InlineData("http://patient.example.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:pass@patient.example.com")]
    [InlineData("https://patient.example.com/?next=https://evil.example.com")]
    [InlineData("https://patient.example.com/#fragment")]
    [InlineData("not a url")]
    [InlineData("")]
    public void An_unsafe_configured_origin_is_not_offered_for_redirect(string configured)
    {
        AuthHandoff.TryResolveConfiguredBaseUrl(
                Config(configured, configured),
                AuthHandoff.PatientSetting)
            .Should().BeNull();
    }

    [Theory]
    [InlineData("http://localhost:3000")]
    [InlineData("http://127.0.0.1:3001")]
    public void Loopback_http_is_allowed_for_local_development(string configured)
    {
        AuthHandoff.TryResolveConfiguredBaseUrl(
                Config(configured, configured),
                AuthHandoff.PatientSetting)
            .Should().Be(configured);
    }

    [Theory]
    [InlineData("ExternalAuthentication.RoleUnsupported", "role_unsupported")]
    [InlineData("ExternalAuthentication.EmailUnverified", "email_unverified")]
    [InlineData("ExternalAuthentication.ProfileUnavailable", "profile_unavailable")]
    [InlineData("ExternalAuthentication.AccountIncomplete", "account_incomplete")]
    [InlineData("ExternalAuthentication.IdentityTaken", "identity_taken")]
    [InlineData("ExternalAuthentication.RemoteError", "remote_error")]
    public void Every_handoff_error_has_a_public_code(string code, string expectedPublicCode)
    {
        var error = Error.Validation(code, "private text");

        AuthHandoff.PublicErrorCode(error).Should().Be(expectedPublicCode);
    }

    [Theory]
    [InlineData("Users.EmailAlreadyExists")]
    [InlineData("")]
    public void Anything_unknown_collapses_to_the_generic_code(string code) =>
        AuthHandoff.PublicErrorCode(Error.NotFound(code, "ali@example.com is taken"))
            .Should().Be(AuthHandoff.GenericFailureCode);

    [Fact]
    public void A_null_error_still_produces_the_generic_code() =>
        AuthHandoff.PublicErrorCode(null!).Should().Be(AuthHandoff.GenericFailureCode);

    [Fact]
    public void A_failure_redirect_carries_the_code_and_nothing_else()
    {
        var url = AuthHandoff.BuildFailureRedirect(
            "https://patient.example.com",
            AuthHandoff.PublicErrorCode(Error.Conflict("ExternalAuthentication.IdentityTaken", "ali@example.com")));

        url.Should().Be("https://patient.example.com/?authError=identity_taken");
        url.Should().NotContain("ali@example.com");
    }

    [Fact]
    public void A_handoff_redirect_carries_only_the_one_time_token()
    {
        var url = AuthHandoff.BuildHandoffRedirect("https://patient.example.com", "a1b2c3d4");

        url.Should().Be("https://patient.example.com/?temporaryToken=a1b2c3d4");
    }

    [Fact]
    public void A_portal_origin_cannot_be_smuggled_through_a_code()
    {
        var url = AuthHandoff.BuildFailureRedirect(
            "https://patient.example.com",
            AuthHandoff.PublicErrorCode(Error.Validation("evil", "https://evil.example.com")));

        url.Should().EndWith($"?authError={AuthHandoff.GenericFailureCode}");
        url.Should().NotContain("evil.example.com");
    }
}
