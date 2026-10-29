using Application.Services;
using Domain.Enums;
using FluentValidation;

namespace Application.Dtos.ChatDtos;

public class CreateChatRoomRequest
{
	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public ChatRoomVisibility Visibility { get; set; } = ChatRoomVisibility.Public;

	/// <summary>
	/// Optional join passphrase. Supplying one always makes the room private;
	/// a private room without a passphrase is members-only but free to join.
	/// </summary>
	public string? Passphrase { get; set; }

	public DateTime? ScheduledStartAtUtc { get; set; }
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

		// Kept as a length bound only; the passphrase is optional and a present
		// but invalid value is reported by the service, which owns the same bounds
		// through RoomAccessPolicy.
		RuleFor(x => x.Passphrase)
			.MaximumLength(RoomAccessPolicy.MaxPassphraseLength)
			.When(x => !string.IsNullOrEmpty(x.Passphrase));
	}

	private static bool ContainNoControlCharacters(string? value) =>
		value is not null && value.All(character => !char.IsControl(character));

	private static bool ContainOnlySupportedControlCharacters(string? value) =>
		value is null || value.All(character =>
			!char.IsControl(character) || character is '\n' or '\r' or '\t');
}
