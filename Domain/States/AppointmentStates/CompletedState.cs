using Domain.Entities;
using Shared;

namespace Domain.States.AppointmentStates;

public class CompletedState : IAppointmentState
{
	private readonly Appointment _appointment;

	public CompletedState(Appointment appointment)
	{
		_appointment = appointment;
	}

	public Result<Appointment> Cancel(string? cancellationReason)
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot cancel an appointment that is already completed.");
	}

	public Result<Appointment> Confirm()
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot confirm an appointment that is already completed.");
	}

	public Result<Appointment> Complete()
	{
		return Error.Conflict("Appointments.InvalidOperation", "Appointment is already completed.");
	}

	public Result<Appointment> Reject(string? rejectionReason)
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot reject an appointment that is already completed.");
	}
}
