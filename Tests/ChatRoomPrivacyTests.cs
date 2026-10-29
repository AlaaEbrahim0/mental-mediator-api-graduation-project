using System.Reflection;
using Application.Dtos.ChatDtos;
using FluentAssertions;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// A response DTO is serialized to every client that can list rooms, and the
/// passphrase hash is the one secret in the room model. This guard fails the
/// build the moment a property that could carry it is added, rather than
/// relying on a reviewer catching it in a diff.
/// </summary>
public class ChatRoomPrivacyTests
{
    private static readonly Type[] ResponseTypes =
    {
        typeof(ChatRoomResponse),
        typeof(ChatRoomPageResponse),
        typeof(ChatRoomBrowseResponse),
        typeof(ChatRoomMemberResponse),
        typeof(ChatMessageResponse),
        typeof(ChatMessagePageResponse),
        typeof(ChatMessageReportResponse),
    };

    private static IEnumerable<PropertyInfo> ResponseProperties() =>
        ResponseTypes.SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    [Fact]
    public void No_response_property_can_carry_a_hash()
    {
        var offenders = ResponseProperties()
            .Where(property => property.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}");

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void A_password_member_on_a_response_is_only_ever_a_flag()
    {
        var offenders = ResponseProperties()
            .Where(property => property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase))
            .Where(property => property.PropertyType != typeof(bool))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}");

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void The_room_response_exposes_the_passphrase_state_as_a_flag_only()
    {
        var property = typeof(ChatRoomResponse).GetProperty(nameof(ChatRoomResponse.RequiresPassword));

        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(bool));
        property.CanWrite.Should().BeTrue();
    }
}
