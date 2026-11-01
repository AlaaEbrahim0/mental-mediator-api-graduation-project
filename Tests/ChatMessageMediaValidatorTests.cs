using Application.Dtos.ChatDtos;
using FluentAssertions;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// A message is valid with text, with media, or with both, but never with
/// neither. These rules moved from a flat "content is required" check to a
/// media-aware one, so the boundary is asserted directly on the validator that
/// runs on every send.
/// </summary>
public class ChatMessageMediaValidatorTests
{
    private static readonly Guid ClientId = Guid.NewGuid();

    private const string ManagedImage = "https://res.cloudinary.com/demo/image/upload/a.jpg";
    private const string ManagedVideo = "https://res.cloudinary.com/demo/video/upload/a.mp4";

    private static SendChatMessageRequest Request(
        string content = "",
        string? mediaUrl = null,
        string? mediaType = null) =>
        new()
        {
            Content = content,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            ClientMessageId = ClientId,
        };

    private static bool IsValid(SendChatMessageRequest request) =>
        new SendChatMessageRequestValidator().Validate(request).IsValid;

    [Fact]
    public void Text_only_message_is_valid() =>
        IsValid(Request(content: "Feeling better today.")).Should().BeTrue();

    [Fact]
    public void Media_only_message_without_text_is_valid() =>
        IsValid(Request(mediaUrl: ManagedImage, mediaType: "image")).Should().BeTrue();

    [Fact]
    public void Text_with_media_is_valid() =>
        IsValid(Request(content: "Look at this", mediaUrl: ManagedVideo, mediaType: "video")).Should().BeTrue();

    [Fact]
    public void Media_type_is_case_and_space_insensitive() =>
        IsValid(Request(mediaUrl: ManagedImage, mediaType: "  IMAGE ")).Should().BeTrue();

    [Fact]
    public void Empty_message_without_media_is_rejected() =>
        IsValid(Request(content: "   ")).Should().BeFalse();

    [Fact]
    public void Media_type_without_url_is_rejected() =>
        IsValid(Request(mediaType: "image")).Should().BeFalse();

    [Fact]
    public void Media_url_without_type_is_rejected() =>
        IsValid(Request(mediaUrl: ManagedImage)).Should().BeFalse();

    [Theory]
    [InlineData("pdf")]
    [InlineData("audio")]
    [InlineData("gif")]
    public void Unsupported_media_type_is_rejected(string mediaType) =>
        IsValid(Request(mediaUrl: ManagedImage, mediaType: mediaType)).Should().BeFalse();

    [Fact]
    public void Text_over_the_length_cap_is_rejected_even_with_media() =>
        IsValid(Request(content: new string('a', 2001), mediaUrl: ManagedImage, mediaType: "image")).Should().BeFalse();

    [Fact]
    public void Missing_client_message_id_is_rejected()
    {
        var request = Request(content: "hi");
        request.ClientMessageId = Guid.Empty;

        IsValid(request).Should().BeFalse();
    }
}
