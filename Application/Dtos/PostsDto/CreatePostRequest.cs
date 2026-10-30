using System.ComponentModel;
using Application.Community;
using Application.Utilities;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Dtos.PostsDto;
public class CreatePostRequest
{
	[DefaultValue("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras laoreet luctus ex. Praesent vel ligula ut neque ullamcorper placerat ac.")]
	public string? Title { get; set; }

	[DefaultValue("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras laoreet luctus ex. Praesent vel ligula ut neque ullamcorper placerat ac.")]
	public string? Content { get; set; }

	/// <summary>One of <see cref="PostTaxonomy.Categories"/>.</summary>
	public string? Category { get; set; }

	/// <summary>Comma-separated selection from <see cref="PostTaxonomy.Tags"/>.</summary>
	public string? Tags { get; set; }

	/// <summary>JSON array of media already uploaded through POST /api/posts/media.</summary>
	public string? Media { get; set; }

	public PostVisibility Visibility { get; set; } = PostVisibility.Public;

	public bool IsAnonymous { get; set; }

	public IFormFile? PhotoPost { get; set; }
}

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{

	public CreatePostRequestValidator()
	{
		RuleFor(x => x.Content)
			.NotEmpty()
			.Length(1, 10000);

		// A title is optional so short social updates do not need one; long
		// clinical write-ups still use it.
		RuleFor(x => x.Title)
			.MaximumLength(2000);

		RuleFor(x => x.Category)
			.NotEmpty()
			.Must(PostTaxonomy.IsCategory)
			.WithMessage("Choose one of the predefined categories.");

		RuleFor(x => x.Tags)
			.Custom((tags, context) =>
			{
				if (!PostTaxonomy.TryNormalizeTags(PostTaxonomy.SplitTags(tags), out _, out var error))
				{
					context.AddFailure("Tags", error!);
				}
			});

		RuleFor(x => x.Media)
			.Custom((media, context) =>
			{
				if (!PostMediaCodec.TryParse(media, out _, out var error))
				{
					context.AddFailure("Media", error!);
				}
			});

		RuleFor(x => x.Visibility)
			.Must(visibility => visibility != PostVisibility.SpecificGroup)
			.WithMessage("Posts for a specific group are not available yet.");

		RuleFor(x => x.PhotoPost)
			.SetValidator(new PhotoValidator()!);

	}
}
