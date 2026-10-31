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

	/// <summary>
	/// Null on an edit means "keep the existing audience". This prevents an
	/// older edit client from silently widening a restricted post.
	/// </summary>
	public PostVisibility? Visibility { get; set; }

	/// <summary>Required for SpecificGroup; a chat room the author belongs to.</summary>
	public int? TargetGroupId { get; set; }

	/// <summary>Null on an edit preserves the stored anonymity preference.</summary>
	public bool? IsAnonymous { get; set; }

	/// <summary>
	/// Drafts may be incomplete and are readable only by their owner and
	/// administrators. Publishing the same record sets this back to false.
	/// </summary>
	public bool IsDraft { get; set; }

	public IFormFile? PhotoPost { get; set; }
}

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{

	public CreatePostRequestValidator()
	{
		// The composer mirrors these limits; published posts must be complete,
		// while a draft may be saved with only some fields filled in.
		RuleFor(x => x.Content)
			.NotEmpty()
			.Length(1, PostTaxonomy.MaxContentLength)
			.When(x => !x.IsDraft);

		RuleFor(x => x.Content)
			.MaximumLength(PostTaxonomy.MaxContentLength);

		RuleFor(x => x.Title)
			.MaximumLength(PostTaxonomy.MaxTitleLength);

		RuleFor(x => x.Category)
			.NotEmpty()
			.When(x => !x.IsDraft)
			.WithMessage("Choose one of the predefined categories.");

		RuleFor(x => x.Category)
			.Must(category => string.IsNullOrWhiteSpace(category) || PostTaxonomy.IsCategory(category))
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

		RuleFor(x => x.TargetGroupId)
			.NotNull().WithMessage("Choose a group for this audience.")
			.GreaterThan(0).WithMessage("Choose a valid group.")
			.When(x => x.Visibility == PostVisibility.SpecificGroup && !x.IsDraft);

		RuleFor(x => x.TargetGroupId)
			.Must(groupId => groupId is null || groupId > 0)
			.WithMessage("Choose a valid group.");

		RuleFor(x => x.TargetGroupId)
			.Null()
			.When(x => x.Visibility != PostVisibility.SpecificGroup && !x.IsDraft)
			.WithMessage("A target group is only valid for the Specific Group audience.");

		RuleFor(x => x.PhotoPost)
			.SetValidator(new PhotoValidator()!);

	}
}
