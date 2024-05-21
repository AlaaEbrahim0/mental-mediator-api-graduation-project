using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;

namespace Domain.Entities;
public class Comment
{
	public int Id { get; set; }
	public int PostId { get; set; }
	public string? AppUserId { get; set; }
	public Post? Post { get; set; }
	public BaseUser? AppUser { get; set; }

	public string? Content { get; set; }
	public DateTime CommentedAt { get; set; }

	public List<Reply> Replies { get; set; } = new();

	[NotMapped]
	public string? Username { get; set; }

}

public class CommentValidator : AbstractValidator<Comment>
{
	public CommentValidator()
	{
		RuleFor(x => x.Content)
			.NotNull()
			.Length(1, 2000);

		RuleFor(x => x.AppUserId)
			.NotNull()
			.NotEmpty();
	}
}
