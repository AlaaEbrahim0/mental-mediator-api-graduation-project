using Domain.Entities;
using Shared;

namespace Domain.States.AppointmentStates;

public class CancelledState : IAppointmentState
{
	private readonly Appointment _appointment;

	public CancelledState(Appointment appointment)
	{
		_appointment = appointment;
	}

	public Result<Appointment> Cancel(string? cancellationReason)
	{
		return Error.Conflict("Appointments.InvalidOperation", "Appointment is already cancelled.");
	}

	public Result<Appointment> Confirm()
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot confirm an appointment that is cancelled.");
	}

	public Result<Appointment> Complete()
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot complete an appointment that is cancelled.");
	}

	public Result<Appointment> Reject(string? rejectionReason)
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot reject an appointment that is cancelled.");
	}
}