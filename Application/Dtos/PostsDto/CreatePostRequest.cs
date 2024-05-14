using System.ComponentModel;
using FluentValidation;

namespace Shared.PostsDto;
public class CreatePostRequest
{
	[DefaultValue("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras laoreet luctus ex. Praesent vel ligula ut neque ullamcorper placerat ac.")]
	public string? Title { get; set; }

	[DefaultValue("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras laoreet luctus ex. Praesent vel ligula ut neque ullamcorper placerat ac.")]
	public string? Content { get; set; }

	public bool IsAnonymous { get; set; }
}

public class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
	public CreatePostRequestValidator()
	{
		RuleFor(x => x.Content)
			.NotEmpty()
			.NotNull()
			.Length(1, 4000);

		RuleFor(x => x.Title)
			.NotEmpty()
			.NotNull()
			.Length(1, 1000);
	}
}