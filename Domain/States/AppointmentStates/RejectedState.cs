using Domain.Entities;
using Shared;
// ReSharper disable All

namespace Domain.States.AppointmentStates;

public class RejectedState : IAppointmentState
{
	private readonly Appointment _appointment;

	public RejectedState(Appointment appointment)
	{
		_appointment = appointment;
	}

	public Result<Appointment> Cancel(string? cancellationReason)
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot cancel an appointment that is already rejected.");
	}

	public Result<Appointment> Confirm()
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot confirm an appointment that is already rejected.");
	}

	public Result<Appointment> Complete()
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot complete an appointment that is already rejected.");
	}

	public Result<Appointment> Reject(string? rejectionReason)
	{
		return Error.Conflict("Appointments.InvalidOperation", "Appointment is already rejected.");
	}
}
