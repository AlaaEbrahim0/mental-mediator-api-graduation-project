using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;

namespace Domain.Entities;

public class Post
{
	public int Id { get; set; }

	public string? AppUserId { get; set; }

	[NotMapped]
	public string? Username { get; set; }

	public string? Title { get; set; }

	public string? Content { get; set; }

	public bool IsAnonymous { get; set; }

	public BaseUser? AppUser { get; set; }
	public DateTime PostedOn { get; set; }
	public List<Comment> Comments { get; set; } = new();
}
public class PostValidator : AbstractValidator<Post>
{
	public PostValidator()
	{
		RuleFor(x => x.Title)
			.Length(1, 500);

		RuleFor(x => x.Content)
			.Length(1, 2000);

		RuleFor(x => x.AppUserId)
			.NotEmpty();
	}
}
