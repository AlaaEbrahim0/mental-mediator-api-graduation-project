namespace API.Configurations;

public static class RateLimitPolicies
{
    public const string ChatMessages = "chat-messages";
    public const string ChatRoomCreation = "chat-room-creation";
    public const string ChatReports = "chat-reports";

    /// <summary>
    /// Per-IP fixed-window limiter for authentication endpoints (sign-in,
    /// register, password reset, email confirmation send). Protects against
    /// brute-force and account-enumeration probing without locking out
    /// legitimate users sharing a NAT gateway.
    /// </summary>
    public const string Auth = "auth";

    /// <summary>
    /// The three Google sign-in navigations (challenge, callback, token exchange).
    /// They are deliberately not on <see cref="Auth"/>: a single browser round
    /// trip hits all three within seconds, and a clinic or campus shares one IP, so
    /// the credential budget would 429 a legitimate sign-in mid-redirect. The
    /// response to a rejected top-level navigation is a raw status code the user
    /// cannot read, so this window is generous and only aims at automation.
    ///
    /// Scope note: <c>[EnableRateLimiting]</c> is endpoint metadata, so it covers
    /// the three controller routes only. The handler path <c>/signin-google</c>
    /// is answered by the authentication middleware, which runs before
    /// <c>UseRateLimiter</c>, and is not limited (verified against a local host).
    /// </summary>
    public const string OAuthHandoff = "oauth-handoff";
}
