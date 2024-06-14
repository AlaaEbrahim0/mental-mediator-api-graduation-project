using Domain.Entities;
using Domain.Enums;
using Shared;

namespace Domain.States.AppointmentStates;

public class PendingState : IAppointmentState
{
	private readonly Appointment _appointment;

	public PendingState(Appointment appointment)
	{
		_appointment = appointment;
	}

	public Result<Appointment> Cancel(string? cancellationReason)
	{
		_appointment.Status = AppointmentStatus.Cancelled;
		_appointment.CancellationReason = cancellationReason;
		return _appointment;
	}

	public Result<Appointment> Confirm()
	{
		_appointment.Status = AppointmentStatus.Confirmed;
		return _appointment;
	}

	public Result<Appointment> Complete()
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot complete an appointment in the pending state.");
	}

	public Result<Appointment> Reject(string? rejectionReason)
	{
		_appointment.Status = AppointmentStatus.Rejected;
		_appointment.RejectionReason = rejectionReason;
		return _appointment;
	}
}
