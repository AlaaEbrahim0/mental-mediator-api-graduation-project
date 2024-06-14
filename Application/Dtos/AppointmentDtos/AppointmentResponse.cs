using Domain.Enums;

namespace Application.Dtos.AppointmentDtos;

public class AppointmentResponse
{
	public int Id { get; set; }
	public string UserId { get; set; } = string.Empty;
	public string DoctorId { get; set; } = string.Empty;
	public DateTime StartTime { get; set; }
	public TimeSpan Duration { get; set; }

	public string ClientName { get; set; } = string.Empty;
	public string ClientEmail { get; set; } = string.Empty;
	public string? ClientPhotoUrl { get; set; }


	public string DoctorName { get; set; } = string.Empty;
	public string DoctorEmail { get; set; } = string.Empty;
	public string? DoctorPhotoUrl { get; set; }

	public DateTime EndTime => StartTime + Duration;

	public AppointmentStatus Status { get; set; }
	public string Location { get; set; } = string.Empty;
	public string? CancellationReason { get; set; }
	public string? RejectionReason { get; set; }
}