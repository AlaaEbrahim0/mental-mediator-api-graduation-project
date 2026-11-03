using System.ComponentModel;
using FluentValidation;

namespace Application.Dtos.CommentsDtos;

public class CreateCommentRequest
{
	[DefaultValue("Comment Example")]
	public string Content { get; set; } = string.Empty;

	/// <summary>URL returned by the managed community media upload endpoint.</summary>
	public string? MediaUrl { get; set; }

	/// <summary>"image" or "video". Required when MediaUrl is set.</summary>
	public string? MediaType { get; set; }
}

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
	public CreateCommentRequestValidator()
	{
		// A comment is valid when it carries text or media. The text length
		// rules still apply whenever text is present.
		RuleFor(x => x.Content)
			.MaximumLength(2000)
			.Must((model, content) => HasMedia(model) || !string.IsNullOrWhiteSpace(content))
			.WithMessage("Comment content cannot be empty or whitespace unless media is attached.");

		RuleFor(x => x.MediaUrl)
			.Must((model, url) => !string.IsNullOrWhiteSpace(url))
			.WithMessage("A media type requires a media URL.")
			.When(x => HasMediaType(x));

		RuleFor(x => x.MediaType)
			.Must(IsImageOrVideo)
			.WithMessage("Media type must be 'image' or 'video'.")
			.When(x => HasMedia(x));
	}

	private static bool HasMediaType(CreateCommentRequest model) =>
		!string.IsNullOrWhiteSpace(model.MediaType);

	private static bool HasMedia(CreateCommentRequest model) =>
		!string.IsNullOrWhiteSpace(model.MediaUrl) || HasMediaType(model);

	private static bool IsImageOrVideo(string? type)
	{
		var normalized = type?.Trim().ToLowerInvariant();
		return normalized is "image" or "video";
	}
}
