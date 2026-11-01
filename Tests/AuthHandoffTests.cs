using FluentAssertions;
using Infrastructure.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Pins how the portal origin is chosen for confirmation and reset
/// links: from the account's roles, never from a caller-supplied
/// destination, and only when the configured URL is safe to redirect to.
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
}
