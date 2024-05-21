using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;

namespace Domain.Entities;

public class Reply
{
	public int Id { get; set; }
	public int CommentId { get; set; }
	public string? AppUserId { get; set; }
	public string? Content { get; set; }
	public DateTime RepliedAt { get; set; }

	public BaseUser? AppUser { get; set; }
	public Comment? Comment { get; set; }

	[NotMapped]
	public string? Username { get; set; }
}
public class ReplyValidator : AbstractValidator<Comment>
{
	public ReplyValidator()
	{
		RuleFor(x => x.Content)
			.NotNull()
			.Length(1, 2000);

		RuleFor(x => x.AppUserId)
			.NotNull()
			.NotEmpty();
	}
}
