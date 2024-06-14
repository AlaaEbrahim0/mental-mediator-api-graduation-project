using Domain.Entities;
using Domain.Enums;
using Shared;
namespace Domain.States.AppointmentStates;


public class ConfirmedState : IAppointmentState
{
	private readonly Appointment _appointment;

	public ConfirmedState(Appointment appointment)
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
		return Error.Conflict("Appointments.InvalidOperation", "Appointment is already confirmed.");
	}

	public Result<Appointment> Complete()
	{
		_appointment.Status = AppointmentStatus.Completed;
		return _appointment;
	}

	public Result<Appointment> Reject(string? rejectionReason)
	{
		return Error.Conflict("Appointments.InvalidOperation", "Cannot reject an appointment that is already confirmed.");
	}
}
