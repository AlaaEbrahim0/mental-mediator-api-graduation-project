using System.ComponentModel;
using Application.Utilities;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Dtos.PostsDto;
public class CreatePostRequest
{
	[DefaultValue("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras laoreet luctus ex. Praesent vel ligula ut neque ullamcorper placerat ac.")]
	public string? Title { get; set; }

	[DefaultValue("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras laoreet luctus ex. Praesent vel ligula ut neque ullamcorper placerat ac.")]
	public string? Content { get; set; }

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

		RuleFor(x => x.Title)
			.NotEmpty()
			.Length(1, 2000);

		RuleFor(x => x.PhotoPost)
			.SetValidator(new PhotoValidator()!);

	}
}
