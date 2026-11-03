using System.ComponentModel;
using Domain.Enums;
using FluentValidation;

namespace Application.Dtos.ReactionsDto;

public class ReactionSummaryResponse
{
	public string Reaction { get; set; } = string.Empty;

	public int Count { get; set; }
}

public class SetReactionRequest
{
	[DefaultValue(ReactionKinds.Like)]
	public string Reaction { get; set; } = ReactionKinds.Like;
}

public class SetReactionRequestValidator : AbstractValidator<SetReactionRequest>
{
	public SetReactionRequestValidator()
	{
		RuleFor(x => x.Reaction)
			.Must(ReactionKinds.IsValid)
			.WithMessage("Reaction must be one of: like, love, haha, wow, sad.");
	}
}
