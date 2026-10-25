using FluentValidation;

namespace Application.Dtos.ChatDtos;

public class CreateChatRoomRequest
{
	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;
}

public class CreateChatRoomRequestValidator : AbstractValidator<CreateChatRoomRequest>
{
	public CreateChatRoomRequestValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.Length(1, 100)
			.Must(ContainNoControlCharacters)
			.WithMessage("Room name cannot contain control characters.");

		RuleFor(x => x.Description)
			.MaximumLength(500)
			.Must(ContainOnlySupportedControlCharacters)
			.WithMessage("Room description contains unsupported control characters.");
	}

	private static bool ContainNoControlCharacters(string? value) =>
		value is not null && value.All(character => !char.IsControl(character));

	private static bool ContainOnlySupportedControlCharacters(string? value) =>
		value is null || value.All(character =>
			!char.IsControl(character) || character is '\n' or '\r' or '\t');
}
