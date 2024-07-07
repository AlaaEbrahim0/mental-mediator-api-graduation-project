using Domain.Enums;
using FluentValidation;

namespace Application.Dtos.UserDtos;

public class UpdateDoctorInfoRequest : UpdateUserInfoRequest
{
	public string? Biography { get; set; }
	public string? City { get; set; }
	public string? Location { get; set; }
	public decimal SessionFees { get; set; }
	public DoctorSpecialization Specialization { get; set; }
}

public class UpdateDoctorInfoRequestValidator : AbstractValidator<UpdateDoctorInfoRequest>
{
	public UpdateDoctorInfoRequestValidator()
	{
		RuleFor(x => x.Biography)
			.MaximumLength(1000)
			.WithMessage("Biography cannot be longer than 1000 characters.");

		RuleFor(x => x.City)
			.NotEmpty()
			.WithMessage("City is required.")
			.MaximumLength(100)
			.WithMessage("City cannot be longer than 100 characters.");

		RuleFor(x => x.Location)
			.NotEmpty()
			.WithMessage("Location is required.")
			.MaximumLength(200)
			.WithMessage("Location cannot be longer than 200 characters.");

		RuleFor(x => x.SessionFees)
			.GreaterThanOrEqualTo(0)
			.WithMessage("Session fees must be a positive number.");

		RuleFor(x => x.Specialization)
			.IsInEnum()
			.WithMessage("Specialization is required and must be a valid value.");
	}
}