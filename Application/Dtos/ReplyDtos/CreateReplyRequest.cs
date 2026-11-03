using System.ComponentModel;
using FluentValidation;

namespace Application.Dtos.ReplyDtos;

public class CreateReplyRequest
{
	[DefaultValue("Reply Example")]
	public string Content { get; set; } = string.Empty;

	/// <summary>URL returned by the managed community media upload endpoint.</summary>
	public string? MediaUrl { get; set; }

	/// <summary>"image" or "video". Required when MediaUrl is set.</summary>
	public string? MediaType { get; set; }
}

public class CreateReplyRequestValidator : AbstractValidator<CreateReplyRequest>
{
	public CreateReplyRequestValidator()
	{
		RuleFor(x => x.Content)
			.MaximumLength(2000)
			.Must((model, content) => HasMedia(model) || !string.IsNullOrWhiteSpace(content))
			.WithMessage("Reply content cannot be empty or whitespace unless media is attached.");

		RuleFor(x => x.MediaUrl)
			.Must((model, url) => !string.IsNullOrWhiteSpace(url))
			.WithMessage("A media type requires a media URL.")
			.When(x => HasMediaType(x));

		RuleFor(x => x.MediaType)
			.Must(IsImageOrVideo)
			.WithMessage("Media type must be 'image' or 'video'.")
			.When(x => HasMedia(x));
	}

	private static bool HasMediaType(CreateReplyRequest model) =>
		!string.IsNullOrWhiteSpace(model.MediaType);

	private static bool HasMedia(CreateReplyRequest model) =>
		!string.IsNullOrWhiteSpace(model.MediaUrl) || HasMediaType(model);

	private static bool IsImageOrVideo(string? type)
	{
		var normalized = type?.Trim().ToLowerInvariant();
		return normalized is "image" or "video";
	}
}
