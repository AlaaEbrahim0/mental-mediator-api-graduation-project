using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

public class Appointment
{
	public int Id { get; set; }
	public string UserId { get; set; } = string.Empty;
	public BaseUser? User { get; set; }
	public string DoctorId { get; set; } = string.Empty;
	public Doctor? Doctor { get; set; }
	public DateTime StartTime { get; set; }
	public TimeSpan Duration { get; set; }

	[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
	public DateTime AppointmentEndTime => StartTime + Duration;

	public AppointmentStatus Status { get; set; }
	public string Location { get; set; } = string.Empty;
	public string? Reason { get; set; } = string.Empty;
	public string? CancellationReason { get; set; }
	public string? RejectionReason { get; set; }
}
