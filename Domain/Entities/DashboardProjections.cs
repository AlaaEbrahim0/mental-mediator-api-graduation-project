using Domain.Enums;

namespace Domain.Entities;

/// <summary>Per-day completed/cancelled counts for the dashboard trend.</summary>
public sealed record DashboardDayCount(DateTime DateUtc, int Completed, int Cancelled);

/// <summary>A row in the doctor's upcoming-appointments list.</summary>
public sealed record DashboardUpcomingAppointment(
	int AppointmentId,
	string PatientName,
	DateTime StartTimeUtc,
	AppointmentStatus Status);
