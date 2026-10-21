using FluentValidation;

namespace Application.Dtos.UserDtos;

public class UpdateUserInfoRequest : BaseUpdateUserInfoRequest
{

}

public class UpdateUserInfoRequestValidator : AbstractValidator<UpdateUserInfoRequest>
{
	public UpdateUserInfoRequestValidator() => Include(new BaseUpdateUserInfoRequestValidator());
}

