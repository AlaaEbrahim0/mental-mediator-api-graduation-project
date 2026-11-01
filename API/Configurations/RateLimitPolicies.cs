namespace API.Configurations;

public static class RateLimitPolicies
{
    public const string ChatMessages = "chat-messages";
    public const string ChatRoomCreation = "chat-room-creation";
    public const string ChatReports = "chat-reports";

    /// <summary>
    /// Caps passphrase guessing. Deliberately wider than the account-password
    /// window because a legitimate joiner retypes a passphrase they mistyped,
    /// but narrow enough that the throttle in <c>RoomJoinThrottle</c> is the
    /// thing that actually stops a determined guesser first.
    /// </summary>
    public const string ChatRoomJoin = "chat-room-join";

    /// <summary>
    /// Per-IP fixed-window limiter for authentication endpoints (sign-in,
    /// register, password reset, email confirmation send). Protects against
    /// brute-force and account-enumeration probing without locking out
    /// legitimate users sharing a NAT gateway.
    /// </summary>
    public const string Auth = "auth";
}
