﻿using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

public class Appointment
{
	public int Id { get; set; }
	public string UserId { get; set; } = string.Empty;
	public User User { get; set; } = null!;
	public string DoctorId { get; set; } = string.Empty;
	public Doctor? Doctor { get; set; } = null!;
	public DateTime StartTime { get; set; }
	public TimeSpan Duration { get; set; }

	[NotMapped]
	public string ClientName { get; set; } = null!;

	[NotMapped]
	public string? ClientPhotoUrl { get; set; }

	[NotMapped]
	public string? DoctorPhotoUrl { get; set; }

	[NotMapped]
	public string DoctorName { get; set; } = null!;


	[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
	public DateTime EndTime => StartTime + Duration;

	public AppointmentStatus Status { get; set; }
	public string Location { get; set; } = null!;
	public string? Reason { get; set; }
	public string? CancellationReason { get; set; }
	public string? RejectionReason { get; set; }
}
