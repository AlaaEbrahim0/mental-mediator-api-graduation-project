using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Domain.States.AppointmentStates;
using Shared;

namespace Domain.Entities;

public class Appointment
{
	public int Id { get; set; }
	public string UserId { get; set; } = string.Empty;
	public User User { get; set; } = null!;

	public string DoctorId { get; set; } = string.Empty;
	public Doctor Doctor { get; set; } = null!;
	public DateTime StartTime { get; set; }
	public TimeSpan Duration { get; set; }

	[NotMapped]
	public string ClientName { get; set; } = null!;

	[NotMapped]
	public string? ClientPhotoUrl { get; set; }

	[NotMapped]
	public string ClientEmail { get; set; } = null!;

	[NotMapped]
	public string? DoctorPhotoUrl { get; set; }

	[NotMapped]
	public string DoctorName { get; set; } = null!;

	[NotMapped]
	public string DoctorEmail { get; set; } = null!;

	[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
	public DateTime EndTime => StartTime + Duration;

	public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
	public string Location { get; set; } = null!;
	public string? Reason { get; set; }
	public string? CancellationReason { get; set; }
	public string? RejectionReason { get; set; }

	[NotMapped]
	private IAppointmentState State => Status switch
	{
		AppointmentStatus.Confirmed => new ConfirmedState(this),
		AppointmentStatus.Cancelled => new CancelledState(this),
		AppointmentStatus.Completed => new CompletedState(this),
		AppointmentStatus.Rejected => new RejectedState(this),
		_ => new PendingState(this)
	};

	public Result<Appointment> Cancel(string? cancellationReason) => State.Cancel(cancellationReason);
	public Result<Appointment> Confirm() => State.Confirm();
	public Result<Appointment> Complete() => State.Complete();
	public Result<Appointment> Reject(string? rejectionReason) => State.Reject(rejectionReason);
}

public interface IAppointmentState
{
	Result<Appointment> Cancel(string? cancellationReason);
	Result<Appointment> Confirm();
	Result<Appointment> Reject(string? rejectionReason);
	Result<Appointment> Complete();
}
