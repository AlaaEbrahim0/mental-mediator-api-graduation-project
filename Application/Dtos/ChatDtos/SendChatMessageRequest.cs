using FluentValidation;

namespace Application.Dtos.ChatDtos;

public class SendChatMessageRequest
{
	public string Content { get; set; } = string.Empty;

	/// <summary>URL returned by the room media upload endpoint. Optional.</summary>
	public string? MediaUrl { get; set; }

	/// <summary>"image" or "video". Required when MediaUrl is set.</summary>
	public string? MediaType { get; set; }

	public Guid ClientMessageId { get; set; }
}

public class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
	public SendChatMessageRequestValidator()
	{
		// A message is valid when it carries text or media (or both). The text
		// rules always apply; only the "must not be empty" requirement is
		// relaxed when a media item is attached.
		RuleFor(x => x.Content)
			.Must((model, content) => HasMedia(model) || !string.IsNullOrWhiteSpace(content))
			.WithMessage("Message content cannot be empty or whitespace.")
			.Length(0, 2000)
			.Must(ContainNoControlCharacters)
			.WithMessage("Message content contains unsupported control characters.");

		RuleFor(x => x.MediaUrl)
			.Must((model, url) => !string.IsNullOrWhiteSpace(url))
			.WithMessage("A media type requires a media URL.")
			.When(x => HasMediaType(x));

		RuleFor(x => x.MediaType)
			.Must(IsImageOrVideo)
			.WithMessage("Media type must be 'image' or 'video'.")
			.When(x => HasMedia(x));

		RuleFor(x => x.ClientMessageId)
			.NotEmpty();
	}

	private static bool HasMediaType(SendChatMessageRequest model) =>
		!string.IsNullOrWhiteSpace(model.MediaType);

	private static bool HasMedia(SendChatMessageRequest model) =>
		!string.IsNullOrWhiteSpace(model.MediaUrl) || HasMediaType(model);

	// Trims before comparing so the validator accepts exactly what the send
	// path accepts: the service lower-cases and trims the type as well.
	private static bool IsImageOrVideo(string? type)
	{
		var normalized = type?.Trim().ToLowerInvariant();
		return normalized is "image" or "video";
	}

	private static bool ContainNoControlCharacters(string? content)
	{
		return content is not null && content.All(character =>
			!char.IsControl(character) || character is '\n' or '\r' or '\t');
	}
}
