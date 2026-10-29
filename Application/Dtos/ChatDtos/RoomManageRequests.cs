using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Application.Dtos.ChatDtos;

/// <summary>
/// Body of a join request. Optional: the portals join with no body at all, and
/// a room without a passphrase does not need one.
/// </summary>
public class JoinChatRoomRequest
{
	public string? Passphrase { get; set; }
}

public class SetRoomPassphraseRequest
{
	public string? Passphrase { get; set; }
}

public class SetRoomPassphraseRequestValidator : AbstractValidator<SetRoomPassphraseRequest>
{
	public SetRoomPassphraseRequestValidator()
	{
		RuleFor(x => x.Passphrase)
			.NotEmpty()
			.WithMessage("Use the delete request to remove a room passphrase.");

		RuleFor(x => x.Passphrase)
			.MinimumLength(8)
			.MaximumLength(128);
	}
}

public class UpdateChatRoomRequest
{
	public string? Name { get; set; }

	public string? Description { get; set; }
}

public class UpdateRoomPrivacyRequest
{
	public ChatRoomVisibility Visibility { get; set; }
}

public class UpdateRoomScheduleRequest
{
	/// <summary>Null clears the scheduled start.</summary>
	public DateTime? ScheduledStartAtUtc { get; set; }
}

/// <summary>
/// Multipart body for the room cover. The form field name is matched by the
/// binder, so it is part of the API contract.
/// </summary>
public class UpdateRoomCoverRequest
{
	public IFormFile? Image { get; set; }
}

public class UpdateRoomCoverRequestValidator : AbstractValidator<UpdateRoomCoverRequest>
{
	public UpdateRoomCoverRequestValidator()
	{
		RuleFor(x => x.Image)
			.NotNull()
			.WithMessage("Select an image to upload.");
	}
}
