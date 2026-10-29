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
}
