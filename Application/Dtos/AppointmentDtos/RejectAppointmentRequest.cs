using FluentValidation;

namespace Application.Dtos.AppointmentDtos;
public class RejectAppointmentRequest
{
	public string? RejectionReason { get; set; }
}

public class RejectAppointmentRequestValidator : AbstractValidator<RejectAppointmentRequest>
{
	public RejectAppointmentRequestValidator() => RuleFor(x => x.RejectionReason).MaximumLength(1000);
}
