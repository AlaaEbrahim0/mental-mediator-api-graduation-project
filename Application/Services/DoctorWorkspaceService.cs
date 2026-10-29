using Application.Contracts;
using Application.Dtos.ChatDtos;
using Application.Dtos.DoctorWorkspaceDtos;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Shared;

namespace Application.Services;

/// <summary>
/// Every number on the doctor dashboard is a server-side aggregate. The book
/// ended up here rather than in a controller so the window rules (what counts
/// as active, what counts as an active patient) are unit-testable against a
/// seeded store.
/// </summary>
public class DoctorWorkspaceService : IDoctorWorkspaceService
{
	/// <summary>Matches the booking repo's own 2-day lookback: an appointment
	/// that started yesterday is still "active" today.</summary>
	private const int ActiveLookbackDays = 2;

	private const int AverageWindowDays = 90;
	private const int FeedWindowDays = 30;
	private const int RoomListLimit = 50;
	private const int RecentJoinLimit = 20;
	private const int RecentNoteLimit = 20;
	private const int DueFollowUpLimit = 20;
	private const int MaxLimit = 100;

	private readonly IRepositoryManager _repos;
	private readonly IUserClaimsService _claims;
	private readonly IPatientDirectoryLookup _patients;

	public DoctorWorkspaceService(
		IRepositoryManager repos,
		IUserClaimsService claims,
		IPatientDirectoryLookup patients)
	{
		_repos = repos;
		_claims = claims;
		_patients = patients;
	}

	public async Task<Result<DoctorDashboardResponse>> GetDashboardAsync()
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId))
		{
			return DoctorWorkspaceErrors.NotAvailable();
		}

		var now = DateTime.UtcNow;
		var activeSince = now.AddDays(-ActiveLookbackDays);
		var windowStart = now.AddDays(-AverageWindowDays);

		// Active and upcoming are disjoint: active is what started in the
		// lookback window and has not started "later", upcoming is everything
		// that has not started yet. That keeps the two cards from double-counting.
		var activeConsultations = await _repos.Appointments.CountDoctorAppointmentsAsync(
			doctorId, AppointmentStatus.Confirmed, activeSince, now);
		var upcomingConsultations = await _repos.Appointments.CountDoctorAppointmentsAsync(
			doctorId, AppointmentStatus.Confirmed, now, null);
		var pendingRequests = await _repos.Appointments.CountDoctorAppointmentsAsync(
			doctorId, AppointmentStatus.Pending, activeSince, null);
		var distinctPatients = await _repos.Appointments.CountDistinctPatientsSinceAsync(
			doctorId, windowStart);
		var completedDurations = await _repos.Appointments.GetCompletedDurationsSinceAsync(
			doctorId, windowStart);

		var totalRoomsHosted = await _repos.ChatRooms.CountRoomsCreatedAsync(doctorId, includeArchived: true);
		var activeRoomsHosted = await _repos.ChatRooms.CountRoomsCreatedAsync(doctorId, includeArchived: false);
		var pendingFollowUps = await _repos.DoctorWorkspace.CountOpenFollowUpsAsync(doctorId);

		var hosted = await _repos.ChatRooms.GetRoomsCreatedAsync(
			doctorId,
			includeArchived: false,
			limit: RoomListLimit);

		var liveRooms = hosted
			.Where(room => room.LastMessageAtUtc.HasValue && room.LastMessageAtUtc >= now.AddHours(-24))
			.OrderByDescending(room => room.LastMessageAtUtc)
			.Take(5)
			.ToList();
		var scheduledRooms = hosted
			.Where(room => room.ScheduledStartAtUtc.HasValue && room.ScheduledStartAtUtc >= now)
			.OrderBy(room => room.ScheduledStartAtUtc)
			.Take(5)
			.ToList();

		var mapped = await MapRoomsAsync(doctorId, liveRooms.Concat(scheduledRooms));

		return new DoctorDashboardResponse
		{
			ActiveConsultations = activeConsultations,
			UpcomingConsultations = upcomingConsultations,
			DistinctActivePatients = distinctPatients,
			TotalRoomsHosted = totalRoomsHosted,
			ActiveRoomsHosted = activeRoomsHosted,
			PendingPatientRequests = pendingRequests,
			PendingFollowUps = pendingFollowUps,
			// The system records the scheduled duration, not the actual one;
			// the field is named accordingly and the UI labels it "avg scheduled".
			AverageConsultationDurationMinutes = completedDurations.Count == 0
				? null
				: Math.Round(completedDurations.Average(duration => duration.TotalMinutes), 1),
			AverageConsultationWindowDays = AverageWindowDays,
			LiveRooms = liveRooms.Select(room => mapped[room.Id]).ToList(),
			UpcomingScheduledRooms = scheduledRooms.Select(room => mapped[room.Id]).ToList(),
		};
	}

	public async Task<Result<IReadOnlyList<DoctorActivityItemResponse>>> GetActivityAsync(int limit)
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId))
		{
			return DoctorWorkspaceErrors.NotAvailable();
		}

		limit = Math.Clamp(limit, 1, MaxLimit);

		var now = DateTime.UtcNow;
		var since = now.AddDays(-FeedWindowDays);

		var ownedRooms = await _repos.ChatRooms.GetRoomsCreatedAsync(
			doctorId,
			includeArchived: true,
			limit: RoomListLimit);
		var roomNames = ownedRooms.ToDictionary(room => room.Id, room => room.Name);
		var roomIds = ownedRooms.Select(room => room.Id).ToList();

		var joins = await _repos.ChatRooms.GetRecentJoinsAsync(roomIds, since, RecentJoinLimit);
		var notes = await _repos.DoctorWorkspace.GetRecentNotesAsync(doctorId, since, RecentNoteLimit);
		var dueFollowUps = await _repos.DoctorWorkspace.GetDueOpenFollowUpsAsync(
			doctorId,
			now,
			DueFollowUpLimit);

		var patientIds = notes.Select(note => note.PatientId)
			.Concat(dueFollowUps.Select(followUp => followUp.PatientId))
			.Distinct()
			.ToList();
		var patientNames = await _patients.GetDisplayNamesAsync(patientIds);

		// A room join shows the member's per-room display name, which is the
		// pseudonymous alias for a patient. The directory lookup is deliberately
		// not applied here: the roster itself never reveals that link.
		var items = new List<DoctorActivityItemResponse>();
		items.AddRange(joins
			.Where(member => member.UserId != doctorId)
			.Select(member => new DoctorActivityItemResponse
			{
				Type = "room_join",
				Title = $"New member in {(roomNames.TryGetValue(member.RoomId, out var name) ? name : "your room")}",
				PatientName = member.DisplayName,
				RoomId = member.RoomId,
				OccurredAtUtc = member.JoinedAtUtc,
			}));
		items.AddRange(notes.Select(note => new DoctorActivityItemResponse
		{
			Type = "note_added",
			Title = $"Note added: {note.Title}",
			PatientName = patientNames.GetValueOrDefault(note.PatientId),
			NoteId = note.Id,
			OccurredAtUtc = note.CreatedAtUtc,
		}));
		items.AddRange(dueFollowUps.Select(followUp => new DoctorActivityItemResponse
		{
			Type = "follow_up_due",
			Title = $"Follow-up due: {followUp.Reason}",
			PatientName = patientNames.GetValueOrDefault(followUp.PatientId),
			FollowUpId = followUp.Id,
			OccurredAtUtc = followUp.DueAtUtc ?? followUp.CreatedAtUtc,
		}));

		IReadOnlyList<DoctorActivityItemResponse> feed = items
			.OrderByDescending(item => item.OccurredAtUtc)
			.Take(limit)
			.ToList();

		return Result<IReadOnlyList<DoctorActivityItemResponse>>.Success(feed);
	}

	public async Task<Result<IReadOnlyList<PatientNoteResponse>>> GetNotesAsync(string? patientId, int limit)
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId))
		{
			return DoctorWorkspaceErrors.NotAvailable();
		}

		limit = Math.Clamp(limit, 1, MaxLimit);

		var notes = await _repos.DoctorWorkspace.GetNotesAsync(doctorId, patientId, limit, false);
		var names = await _patients.GetDisplayNamesAsync(
			notes.Select(note => note.PatientId).Distinct().ToList());

		IReadOnlyList<PatientNoteResponse> response = notes
			.Select(note => MapNote(note, names.GetValueOrDefault(note.PatientId)))
			.ToList();

		return Result<IReadOnlyList<PatientNoteResponse>>.Success(response);
	}

	public async Task<Result<PatientNoteResponse>> CreateNoteAsync(CreatePatientNoteRequest request)
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId))
		{
			return DoctorWorkspaceErrors.NotAvailable();
		}

		var patientId = request.PatientId?.Trim();
		if (string.IsNullOrEmpty(patientId) || !await _patients.ExistsAsync(patientId))
		{
			return DoctorWorkspaceErrors.PatientNotFound();
		}

		var title = request.Title?.Trim() ?? string.Empty;
		if (title.Length == 0 || title.Length > 120)
		{
			return DoctorWorkspaceErrors.InvalidNoteTitle();
		}

		var body = request.Body?.Trim() ?? string.Empty;
		if (body.Length == 0 || body.Length > 4000)
		{
			return DoctorWorkspaceErrors.InvalidNoteBody();
		}

		if (request.AppointmentId.HasValue &&
			!await AppointmentBelongsToAsync(request.AppointmentId.Value, doctorId, patientId))
		{
			return DoctorWorkspaceErrors.InvalidAppointment();
		}

		var note = new PatientNote
		{
			DoctorId = doctorId,
			PatientId = patientId,
			AppointmentId = request.AppointmentId,
			Title = title,
			Body = body,
			CreatedAtUtc = DateTime.UtcNow,
		};

		_repos.DoctorWorkspace.CreateNote(note);
		await _repos.SaveAsync();

		var names = await _patients.GetDisplayNamesAsync(new[] { patientId });
		return MapNote(note, names.GetValueOrDefault(patientId));
	}

	public async Task<Result<PatientNoteResponse>> UpdateNoteAsync(int noteId, UpdatePatientNoteRequest request)
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId))
		{
			return DoctorWorkspaceErrors.NotAvailable();
		}

		var note = await _repos.DoctorWorkspace.GetNoteAsync(noteId, doctorId, true);
		if (note is null)
		{
			return DoctorWorkspaceErrors.NoteNotFound();
		}

		if (request.Title is not null)
		{
			var title = request.Title.Trim();
			if (title.Length == 0 || title.Length > 120)
			{
				return DoctorWorkspaceErrors.InvalidNoteTitle();
			}

			note.Title = title;
		}

		if (request.Body is not null)
		{
			var body = request.Body.Trim();
			if (body.Length == 0 || body.Length > 4000)
			{
				return DoctorWorkspaceErrors.InvalidNoteBody();
			}

			note.Body = body;
		}

		note.UpdatedAtUtc = DateTime.UtcNow;
		_repos.DoctorWorkspace.UpdateNote(note);
		await _repos.SaveAsync();

		var names = await _patients.GetDisplayNamesAsync(new[] { note.PatientId });
		return MapNote(note, names.GetValueOrDefault(note.PatientId));
	}

	public async Task<Result<IReadOnlyList<FollowUpResponse>>> GetFollowUpsAsync(FollowUpStatus? status, int limit)
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId))
		{
			return DoctorWorkspaceErrors.NotAvailable();
		}

		limit = Math.Clamp(limit, 1, MaxLimit);

		var followUps = await _repos.DoctorWorkspace.GetFollowUpsAsync(doctorId, status, limit, false);
		var names = await _patients.GetDisplayNamesAsync(
			followUps.Select(followUp => followUp.PatientId).Distinct().ToList());

		IReadOnlyList<FollowUpResponse> response = followUps
			.Select(followUp => MapFollowUp(followUp, names.GetValueOrDefault(followUp.PatientId)))
			.ToList();

		return Result<IReadOnlyList<FollowUpResponse>>.Success(response);
	}

	public async Task<Result<FollowUpResponse>> CreateFollowUpAsync(CreateFollowUpRequest request)
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId))
		{
			return DoctorWorkspaceErrors.NotAvailable();
		}

		var patientId = request.PatientId?.Trim();
		if (string.IsNullOrEmpty(patientId) || !await _patients.ExistsAsync(patientId))
		{
			return DoctorWorkspaceErrors.PatientNotFound();
		}

		var reason = request.Reason?.Trim() ?? string.Empty;
		if (reason.Length == 0 || reason.Length > 200)
		{
			return DoctorWorkspaceErrors.InvalidFollowUpReason();
		}

		var now = DateTime.UtcNow;
		if (request.DueAtUtc.HasValue && request.DueAtUtc.Value < now.AddDays(-1))
		{
			return DoctorWorkspaceErrors.InvalidFollowUpDueDate();
		}

		if (request.AppointmentId.HasValue &&
			!await AppointmentBelongsToAsync(request.AppointmentId.Value, doctorId, patientId))
		{
			return DoctorWorkspaceErrors.InvalidAppointment();
		}

		var followUp = new FollowUp
		{
			DoctorId = doctorId,
			PatientId = patientId,
			AppointmentId = request.AppointmentId,
			Reason = reason,
			Status = FollowUpStatus.Open,
			DueAtUtc = request.DueAtUtc,
			CreatedAtUtc = now,
		};

		_repos.DoctorWorkspace.CreateFollowUp(followUp);
		await _repos.SaveAsync();

		var names = await _patients.GetDisplayNamesAsync(new[] { patientId });
		return MapFollowUp(followUp, names.GetValueOrDefault(patientId));
	}

	public async Task<Result<FollowUpResponse>> UpdateFollowUpAsync(
		int followUpId,
		UpdateFollowUpRequest request)
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId))
		{
			return DoctorWorkspaceErrors.NotAvailable();
		}

		if (!Enum.IsDefined(request.Status))
		{
			return DoctorWorkspaceErrors.InvalidFollowUpStatus();
		}

		var followUp = await _repos.DoctorWorkspace.GetFollowUpAsync(followUpId, doctorId, true);
		if (followUp is null)
		{
			return DoctorWorkspaceErrors.FollowUpNotFound();
		}

		if (followUp.Status != request.Status)
		{
			followUp.Status = request.Status;
			followUp.ResolvedAtUtc = request.Status == FollowUpStatus.Open
				? null
				: DateTime.UtcNow;

			_repos.DoctorWorkspace.UpdateFollowUp(followUp);
			await _repos.SaveAsync();
		}

		var names = await _patients.GetDisplayNamesAsync(new[] { followUp.PatientId });
		return MapFollowUp(followUp, names.GetValueOrDefault(followUp.PatientId));
	}

	private async Task<bool> AppointmentBelongsToAsync(int appointmentId, string doctorId, string patientId)
	{
		var appointment = await _repos.Appointments.GetById(appointmentId, false);

		return appointment is not null &&
			appointment.DoctorId == doctorId &&
			appointment.UserId == patientId;
	}

	private async Task<IReadOnlyDictionary<int, ChatRoomResponse>> MapRoomsAsync(
		string doctorId,
		IEnumerable<ChatRoom> rooms)
	{
		var list = rooms.DistinctBy(room => room.Id).ToList();
		var roomIds = list.Select(room => room.Id).ToList();

		var memberships = await _repos.ChatRooms.GetMembershipsAsync(doctorId, roomIds, false);
		var membershipByRoom = memberships.ToDictionary(member => member.RoomId);
		var counts = await _repos.ChatRooms.GetActiveMemberCountsAsync(roomIds);
		var owners = await _repos.ChatRooms.GetOwnersByRoomIdsAsync(roomIds);

		return list.ToDictionary(room => room.Id, room =>
		{
			membershipByRoom.TryGetValue(room.Id, out var membership);
			counts.TryGetValue(room.Id, out var count);
			owners.TryGetValue(room.Id, out var owner);

			return ChatService.MapRoom(room, membership, count, null, owner);
		});
	}

	private static PatientNoteResponse MapNote(PatientNote note, string? patientName) =>
		new()
		{
			Id = note.Id,
			PatientId = note.PatientId,
			PatientName = patientName,
			AppointmentId = note.AppointmentId,
			Title = note.Title,
			Body = note.Body,
			CreatedAtUtc = note.CreatedAtUtc,
			UpdatedAtUtc = note.UpdatedAtUtc,
		};

	private static FollowUpResponse MapFollowUp(FollowUp followUp, string? patientName) =>
		new()
		{
			Id = followUp.Id,
			PatientId = followUp.PatientId,
			PatientName = patientName,
			AppointmentId = followUp.AppointmentId,
			Reason = followUp.Reason,
			Status = followUp.Status,
			DueAtUtc = followUp.DueAtUtc,
			CreatedAtUtc = followUp.CreatedAtUtc,
			ResolvedAtUtc = followUp.ResolvedAtUtc,
		};
}
