using FluentValidation;

namespace Application.Dtos.ChatDtos;

public class SendChatMessageRequest
{
	public string Content { get; set; } = string.Empty;

	public Guid ClientMessageId { get; set; }
}

public class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
	public SendChatMessageRequestValidator()
	{
		RuleFor(x => x.Content)
			.NotEmpty()
			.Length(1, 2000)
			.Must(content => !string.IsNullOrWhiteSpace(content))
			.WithMessage("Message content cannot be empty or whitespace.")
			.Must(ContainNoControlCharacters)
			.WithMessage("Message content contains unsupported control characters.");

		RuleFor(x => x.ClientMessageId)
			.NotEmpty();
	}

	private static bool ContainNoControlCharacters(string? content)
	{
		return content is not null && content.All(character =>
			!char.IsControl(character) || character is '\n' or '\r' or '\t');
	}
}
