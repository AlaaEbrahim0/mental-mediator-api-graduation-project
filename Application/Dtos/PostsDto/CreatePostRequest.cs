using System.ComponentModel;
using Application.Contracts;
using FluentValidation;

namespace Application.Dtos.PostsDto;
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
	private readonly IHateSpeechDetector hateSpeechDetector;

	public CreatePostRequestValidator(IHateSpeechDetector hateSpeechDetector)
	{
		RuleFor(x => x.Content)
			.NotEmpty()
			.Length(1, 4000);

		RuleFor(x => x.Title)
			.NotEmpty()
			.Length(1, 1000);
		this.hateSpeechDetector = hateSpeechDetector;
	}
}