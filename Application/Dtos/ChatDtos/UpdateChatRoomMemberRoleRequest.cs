using Domain.Enums;
using FluentValidation;

namespace Application.Dtos.ChatDtos;

public class UpdateChatRoomMemberRoleRequest
{
	public ChatRoomRole? Role { get; set; }
}

public class UpdateChatRoomMemberRoleRequestValidator : AbstractValidator<UpdateChatRoomMemberRoleRequest>
{
	public UpdateChatRoomMemberRoleRequestValidator()
	{
		RuleFor(x => x.Role)
			.NotNull()
			.Must(role => role is ChatRoomRole.Member or ChatRoomRole.Moderator)
			.WithMessage("Room members can only be assigned the Member or Moderator role.");
	}
}
