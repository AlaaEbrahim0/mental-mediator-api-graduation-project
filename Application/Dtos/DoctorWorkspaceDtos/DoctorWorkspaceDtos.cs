using Application.Dtos.ChatDtos;
using Domain.Enums;

namespace Application.Dtos.DoctorWorkspaceDtos;

public class DoctorDashboardResponse
{
	/// <summary>Confirmed consultations inside the active window (see
	/// <c>DoctorWorkspaceService.ActiveLookbackDays</c>).</summary>
	public int ActiveConsultations { get; set; }

	public int UpcomingConsultations { get; set; }

	public int DistinctActivePatients { get; set; }

	public int TotalRoomsHosted { get; set; }

	public int ActiveRoomsHosted { get; set; }

	public int PendingPatientRequests { get; set; }

	public int PendingFollowUps { get; set; }

	/// <summary>
	/// Mean of the <em>scheduled</em> durations of completed consultations in
	/// <see cref="AverageConsultationWindowDays"/>. The system does not record
	/// actual session length, so the UI labels this "avg scheduled".
	/// </summary>
	public double? AverageConsultationDurationMinutes { get; set; }

	public int AverageConsultationWindowDays { get; set; }

	public List<ChatRoomResponse> UpcomingScheduledRooms { get; set; } = new();

	/// <summary>Rooms the doctor hosts with recent conversation; member counts,
	/// never "online" users.</summary>
	public List<ChatRoomResponse> LiveRooms { get; set; } = new();

	// ---- Date-range analytics (see RangeDays) -----------------------------

	/// <summary>The reporting window every ranged metric below uses.</summary>
	public int RangeDays { get; set; }

	public int AppointmentsInRange { get; set; }

	public int CompletedInRange { get; set; }

	public int CancelledInRange { get; set; }

	public int PendingInRange { get; set; }

	public int ConfirmedInRange { get; set; }

	public List<DashboardTrendPoint> Trend { get; set; } = new();

	public int FollowUpsOverdue { get; set; }

	public int FollowUpsDueSoon { get; set; }

	public int FollowUpsCompletedInRange { get; set; }

	public int NewPatients { get; set; }

	public int ReturningPatients { get; set; }

	public List<UpcomingAppointmentItem> UpcomingAppointments { get; set; } = new();
}

public sealed class DashboardTrendPoint
{
	public DateTime DateUtc { get; set; }

	public int Completed { get; set; }

	public int Cancelled { get; set; }
}

public sealed class UpcomingAppointmentItem
{
	public int AppointmentId { get; set; }

	public string PatientName { get; set; } = string.Empty;

	public DateTime StartTimeUtc { get; set; }

	public string Status { get; set; } = string.Empty;
}

public class DoctorActivityItemResponse
{
	/// <summary><c>room_join</c>, <c>note_added</c> or <c>follow_up_due</c>.</summary>
	public string Type { get; set; } = string.Empty;

	public string Title { get; set; } = string.Empty;

	public string? Detail { get; set; }

	public string? PatientName { get; set; }

	public int? RoomId { get; set; }

	public int? NoteId { get; set; }

	public int? FollowUpId { get; set; }

	public DateTime OccurredAtUtc { get; set; }
}

public class PatientNoteResponse
{
	public int Id { get; set; }

	public string PatientId { get; set; } = string.Empty;

	public string? PatientName { get; set; }

	public int? AppointmentId { get; set; }

	public string Title { get; set; } = string.Empty;

	public string Body { get; set; } = string.Empty;

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? UpdatedAtUtc { get; set; }

	public bool IsPatientVisible { get; set; }
}

public class CreatePatientNoteRequest
{
	public string? PatientId { get; set; }

	public int? AppointmentId { get; set; }

	public string? Title { get; set; }

	public string? Body { get; set; }

	/// <summary>Share the note with the patient's own history.</summary>
	public bool IsPatientVisible { get; set; }
}

public class UpdatePatientNoteRequest
{
	public string? Title { get; set; }

	public string? Body { get; set; }

	public bool? IsPatientVisible { get; set; }
}

public class FollowUpResponse
{
	public int Id { get; set; }

	public string PatientId { get; set; } = string.Empty;

	public string? PatientName { get; set; }

	public int? AppointmentId { get; set; }

	public string Reason { get; set; } = string.Empty;

	public FollowUpStatus Status { get; set; }

	public DateTime? DueAtUtc { get; set; }

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? ResolvedAtUtc { get; set; }

	public bool IsPatientVisible { get; set; }
}

public class CreateFollowUpRequest
{
	public string? PatientId { get; set; }

	public int? AppointmentId { get; set; }

	public string? Reason { get; set; }

	public DateTime? DueAtUtc { get; set; }

	/// <summary>Share the follow-up instructions with the patient.</summary>
	public bool IsPatientVisible { get; set; }
}

public class UpdateFollowUpRequest
{
	/// <summary>Omitted leaves the current status unchanged.</summary>
	public FollowUpStatus? Status { get; set; }

	public bool? IsPatientVisible { get; set; }
}
