using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Shared;

namespace Application.Services;

/// <summary>
/// Who may enter a room, and what a viewer is allowed to see. Kept free of
/// database and framework calls so the entire join decision — including the
/// order the checks run in, which is what prevents an unjoinable room from
/// being probed — is testable as a function.
/// </summary>
public static class RoomAccessPolicy
{
	public const int MinPassphraseLength = 8;
	public const int MaxPassphraseLength = 128;

	/// <summary>How far ahead a room may be scheduled; keeps the dashboard's
	/// "upcoming" list from filling with placeholders.</summary>
	public static readonly TimeSpan MaxScheduleHorizon = TimeSpan.FromDays(90);

	/// <summary>
	/// The facts about one join request. <see cref="PassphraseMatches"/> is
	/// computed by the caller because hashing lives outside this class.
	/// </summary>
	public readonly record struct JoinAttempt(
		bool IsActiveMember,
		bool PassphraseSupplied,
		bool PassphraseMatches,
		bool BlockedByThrottle);

	public static Result EvaluateJoin(ChatRoom room, JoinAttempt attempt)
	{
		// Archived first: it is the only state that outranks an existing
		// membership, and it matches how joins behaved before passphrases.
		if (room.IsArchived)
		{
			return ChatErrors.RoomArchived();
		}

		// A member is never re-prompted, and is never blocked by a pause, so a
		// locked room stays readable for the people already inside it and a
		// changed passphrase cannot lock out someone who is already a member.
		if (attempt.IsActiveMember)
		{
			return Result.Success();
		}

		if (room.IsLocked)
		{
			return ChatErrors.RoomLocked();
		}

		if (room.PasswordHash is not null)
		{
			if (!attempt.PassphraseSupplied)
			{
				return ChatErrors.RoomPassphraseRequired();
			}

			// Deliberately one error for a wrong guess and for a throttled caller:
			// a failed join must not tell an attacker whether to change the
			// passphrase or change the waiting time.
			if (attempt.BlockedByThrottle || !attempt.PassphraseMatches)
			{
				return ChatErrors.RoomPassphraseIncorrect();
			}
		}

		return Result.Success();
	}

	/// <summary>
	/// Conversation content — description, member roster, messages — belongs to
	/// members for private rooms. Listing the room itself is not a disclosure.
	/// </summary>
	public static bool CanSeeContent(ChatRoom room, bool isActiveMember) =>
		isActiveMember || room.Visibility == ChatRoomVisibility.Public;

	/// <summary>
	/// Redacted as an empty string rather than removed: every portal already
	/// renders <c>room.description || "No description."</c>, so a non-member gets
	/// the same shape they would for a public room without one.
	/// </summary>
	public static string RedactDescription(ChatRoom room, bool canSeeContent) =>
		canSeeContent ? room.Description : string.Empty;

	public static bool IsValidPassphrase(string? passphrase) =>
		!string.IsNullOrWhiteSpace(passphrase) &&
		passphrase.Length >= MinPassphraseLength &&
		passphrase.Length <= MaxPassphraseLength &&
		passphrase.All(character => !char.IsControl(character));

	/// <summary>
	/// Applies a privacy change and an optional new hash. A passphrase always
	/// implies a private room, and a public room never keeps a hash — the two
	/// nonsense states that a caller could otherwise combine.
	/// </summary>
	public static void ApplyPrivacy(ChatRoom room, ChatRoomVisibility? visibility, string? hashedPassphrase)
	{
		if (visibility.HasValue)
		{
			room.Visibility = visibility.Value;
		}

		if (hashedPassphrase is not null)
		{
			room.PasswordHash = hashedPassphrase;
			room.Visibility = ChatRoomVisibility.Private;
		}

		if (room.Visibility == ChatRoomVisibility.Public)
		{
			room.PasswordHash = null;
		}
	}

	public static void ClearPassphrase(ChatRoom room) => room.PasswordHash = null;

	public static Result ValidateScheduledStart(DateTime? scheduledStartUtc, DateTime nowUtc)
	{
		if (!scheduledStartUtc.HasValue)
		{
			return Result.Success();
		}

		if (scheduledStartUtc.Value <= nowUtc || scheduledStartUtc.Value > nowUtc.Add(MaxScheduleHorizon))
		{
			return ChatErrors.InvalidScheduledStart();
		}

		return Result.Success();
	}
}
