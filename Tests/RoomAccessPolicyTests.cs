using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Shared;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// The join decision is a single function with a fixed gate order. These tests
/// pin that order, the errors it returns, and the visibility rule that decides
/// what a non-member may read.
/// </summary>
public class RoomAccessPolicyTests
{
    private static ChatRoom Room(
        ChatRoomVisibility visibility = ChatRoomVisibility.Public,
        bool isArchived = false,
        bool isLocked = false,
        string? passwordHash = null) =>
        new()
        {
            Id = 1,
            Name = "Room",
            Description = "A description",
            Visibility = visibility,
            IsArchived = isArchived,
            IsLocked = isLocked,
            PasswordHash = passwordHash,
        };

    private static RoomAccessPolicy.JoinAttempt Attempt(
        bool isActiveMember = false,
        bool supplied = false,
        bool matches = false,
        bool blocked = false) =>
        new(isActiveMember, supplied, matches, blocked);

    [Fact]
    public void An_archived_room_rejects_even_an_active_member()
    {
        var result = RoomAccessPolicy.EvaluateJoin(
            Room(isArchived: true),
            Attempt(isActiveMember: true));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.RoomArchived");
    }

    [Fact]
    public void An_existing_member_is_never_re_prompted()
    {
        // Even with no passphrase, a wrong one, or a throttle that has already
        // tripped, a member stays a member.
        var result = RoomAccessPolicy.EvaluateJoin(
            Room(visibility: ChatRoomVisibility.Private, passwordHash: "hash"),
            Attempt(isActiveMember: true, supplied: false, matches: false, blocked: true));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void A_locked_room_does_not_lock_out_its_members()
    {
        var result = RoomAccessPolicy.EvaluateJoin(
            Room(isLocked: true),
            Attempt(isActiveMember: true));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void A_locked_room_blocks_a_new_member_even_with_the_right_passphrase()
    {
        var result = RoomAccessPolicy.EvaluateJoin(
            Room(isLocked: true, passwordHash: "hash"),
            Attempt(supplied: true, matches: true));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.RoomLocked");
    }

    [Fact]
    public void A_passphrase_room_asks_for_the_passphrase_before_judging_it()
    {
        var result = RoomAccessPolicy.EvaluateJoin(
            Room(visibility: ChatRoomVisibility.Private, passwordHash: "hash"),
            Attempt(supplied: false));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.RoomPassphraseRequired");
    }

    [Fact]
    public void A_wrong_passphrase_is_rejected()
    {
        var result = RoomAccessPolicy.EvaluateJoin(
            Room(visibility: ChatRoomVisibility.Private, passwordHash: "hash"),
            Attempt(supplied: true, matches: false));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.RoomPassphraseIncorrect");
    }

    [Fact]
    public void A_throttled_caller_gets_the_same_error_as_a_wrong_guess()
    {
        // If these two differed, a failed join would tell an attacker whether
        // to wait or to keep guessing.
        var wrong = RoomAccessPolicy.EvaluateJoin(
            Room(passwordHash: "hash"),
            Attempt(supplied: true, matches: false));
        var throttled = RoomAccessPolicy.EvaluateJoin(
            Room(passwordHash: "hash"),
            Attempt(supplied: true, matches: true, blocked: true));

        throttled.Error.Should().Be(wrong.Error);
    }

    [Fact]
    public void The_right_passphrase_opens_the_room()
    {
        var result = RoomAccessPolicy.EvaluateJoin(
            Room(visibility: ChatRoomVisibility.Private, passwordHash: "hash"),
            Attempt(supplied: true, matches: true));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void A_private_room_without_a_passphrase_joins_freely()
    {
        var result = RoomAccessPolicy.EvaluateJoin(
            Room(visibility: ChatRoomVisibility.Private),
            Attempt());

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(ChatRoomVisibility.Public, false, "A description")]
    [InlineData(ChatRoomVisibility.Public, true, "A description")]
    [InlineData(ChatRoomVisibility.Private, true, "A description")]
    [InlineData(ChatRoomVisibility.Private, false, "")]
    public void A_private_room_hides_its_description_from_non_members(
        ChatRoomVisibility visibility,
        bool isActiveMember,
        string expected)
    {
        var room = Room(visibility: visibility);

        RoomAccessPolicy.RedactDescription(room, RoomAccessPolicy.CanSeeContent(room, isActiveMember))
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("short", false)]
    [InlineData("12345678", true)]
    [InlineData("1234567", false)]
    [InlineData("with\u0007control", false)]
    public void Passphrase_length_is_bounded(string? passphrase, bool expected) =>
        RoomAccessPolicy.IsValidPassphrase(passphrase).Should().Be(expected);

    [Fact]
    public void A_128_character_passphrase_is_allowed_and_129_is_not()
    {
        RoomAccessPolicy.IsValidPassphrase(new string('a', 128)).Should().BeTrue();
        RoomAccessPolicy.IsValidPassphrase(new string('a', 129)).Should().BeFalse();
    }

    [Fact]
    public void A_passphrase_forces_the_room_private()
    {
        var room = Room();

        RoomAccessPolicy.ApplyPrivacy(room, ChatRoomVisibility.Public, "hash");

        room.Visibility.Should().Be(ChatRoomVisibility.Private);
        room.PasswordHash.Should().Be("hash");
    }

    [Fact]
    public void Making_a_room_public_drops_its_passphrase()
    {
        var room = Room(visibility: ChatRoomVisibility.Private, passwordHash: "hash");

        RoomAccessPolicy.ApplyPrivacy(room, ChatRoomVisibility.Public, null);

        room.Visibility.Should().Be(ChatRoomVisibility.Public);
        room.PasswordHash.Should().BeNull();
    }

    [Fact]
    public void A_cleared_passphrase_leaves_a_private_room_private()
    {
        var room = Room(visibility: ChatRoomVisibility.Private, passwordHash: "hash");

        RoomAccessPolicy.ClearPassphrase(room);

        room.PasswordHash.Should().BeNull();
        room.Visibility.Should().Be(ChatRoomVisibility.Private);
    }

    [Fact]
    public void A_scheduled_start_must_be_in_the_future_and_within_the_horizon()
    {
        var now = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

        RoomAccessPolicy.ValidateScheduledStart(null, now).IsSuccess.Should().BeTrue();
        RoomAccessPolicy.ValidateScheduledStart(now.AddDays(1), now).IsSuccess.Should().BeTrue();
        RoomAccessPolicy.ValidateScheduledStart(now.AddDays(90), now).IsSuccess.Should().BeTrue();

        RoomAccessPolicy.ValidateScheduledStart(now.AddMinutes(-1), now).Error.Code
            .Should().Be("Chat.InvalidScheduledStart");
        RoomAccessPolicy.ValidateScheduledStart(now, now).Error.Code
            .Should().Be("Chat.InvalidScheduledStart");
        RoomAccessPolicy.ValidateScheduledStart(now.AddDays(91), now).Error.Code
            .Should().Be("Chat.InvalidScheduledStart");
    }
}
