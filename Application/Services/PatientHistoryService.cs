using Application.Contracts;
using Application.Dtos.HistoryDtos;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Shared;

namespace Application.Services;

/// <summary>
/// Merges the existing appointment, note and follow-up records into one
/// chronological patient history. It does not create a second history store:
/// authorization and visibility are resolved from the records the existing
/// services already own.
/// </summary>
public class PatientHistoryService : IPatientHistoryService
{
	private const int MaxItemsPerSource = 200;

	// The category vocabulary accepted by the endpoints. "all" is the default
	// and returns the merged timeline; the others restrict the query to one
	// record type so a filtered page never loads the other sources.
	public const string CategoryAll = "all";
	public const string CategoryAppointments = "appointments";
	public const string CategoryFollowUps = "follow_ups";
	public const string CategoryNotes = "notes";

	private readonly IRepositoryManager _repos;
	private readonly IUserClaimsService _claims;
	private readonly IPatientDirectoryLookup _patients;

	public PatientHistoryService(
		IRepositoryManager repos,
		IUserClaimsService claims,
		IPatientDirectoryLookup patients)
	{
		_repos = repos;
		_claims = claims;
		_patients = patients;
	}

	public async Task<Result<PatientHistoryResponse>> GetMyHistoryAsync(
		int pageNumber,
		int pageSize,
		string? category = null,
		DateTime? fromUtc = null,
		DateTime? toUtc = null)
	{
		var patientId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(patientId))
		{
			return HistoryErrors.NotFound();
		}

		var normalized = NormalizeCategory(category);

		var appointments = IncludesAppointments(normalized)
			? await _repos.Appointments.GetHistoryForPatientAsync(patientId, doctorId: null, MaxItemsPerSource, false)
			: Array.Empty<Appointment>();
		var notes = IncludesNotes(normalized)
			? await _repos.DoctorWorkspace.GetVisibleNotesForPatientAsync(patientId, MaxItemsPerSource)
			: Array.Empty<PatientNote>();
		var followUps = IncludesFollowUps(normalized)
			? await _repos.DoctorWorkspace.GetVisibleFollowUpsForPatientAsync(patientId, MaxItemsPerSource)
			: Array.Empty<FollowUp>();

		var names = await _patients.GetDisplayNamesAsync(new[] { patientId });
		return await BuildResponseAsync(
			patientId,
			names.GetValueOrDefault(patientId),
			appointments,
			notes,
			followUps,
			normalized,
			fromUtc,
			toUtc,
			pageNumber,
			pageSize);
	}

	public async Task<Result<PatientHistoryResponse>> GetPatientHistoryAsync(
		string patientId,
		int pageNumber,
		int pageSize,
		string? category = null,
		DateTime? fromUtc = null,
		DateTime? toUtc = null)
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId) ||
			string.IsNullOrWhiteSpace(patientId))
		{
			return HistoryErrors.NotFound();
		}

		var normalized = NormalizeCategory(category);

		var appointments = IncludesAppointments(normalized)
			? await _repos.Appointments.GetHistoryForPatientAsync(patientId, doctorId, MaxItemsPerSource, false)
			: Array.Empty<Appointment>();
		var notes = IncludesNotes(normalized)
			? await _repos.DoctorWorkspace.GetNotesForDoctorPatientAsync(doctorId, patientId, MaxItemsPerSource)
			: Array.Empty<PatientNote>();
		var followUps = IncludesFollowUps(normalized)
			? await _repos.DoctorWorkspace.GetFollowUpsForDoctorPatientAsync(doctorId, patientId, MaxItemsPerSource)
			: Array.Empty<FollowUp>();

		// A category filter legitimately returns nothing even when a care
		// relationship exists, so the relationship is proven by an unfiltered
		// probe before answering 404: without records of any kind there is no
		// relationship to hide behind.
		if (appointments.Count == 0 && notes.Count == 0 && followUps.Count == 0 &&
			(normalized == CategoryAll || !await HasAnyRelationshipAsync(doctorId, patientId)))
		{
			return HistoryErrors.NotFound();
		}

		var names = await _patients.GetDisplayNamesAsync(new[] { patientId });
		return await BuildResponseAsync(
			patientId,
			names.GetValueOrDefault(patientId),
			appointments,
			notes,
			followUps,
			normalized,
			fromUtc,
			toUtc,
			pageNumber,
			pageSize);
	}

	private async Task<bool> HasAnyRelationshipAsync(string doctorId, string patientId)
	{
		var anyAppointment = await _repos.Appointments.GetHistoryForPatientAsync(patientId, doctorId, 1, false);
		if (anyAppointment.Count > 0)
		{
			return true;
		}

		var anyNote = await _repos.DoctorWorkspace.GetNotesForDoctorPatientAsync(doctorId, patientId, 1);
		if (anyNote.Count > 0)
		{
			return true;
		}

		var anyFollowUp = await _repos.DoctorWorkspace.GetFollowUpsForDoctorPatientAsync(doctorId, patientId, 1);
		return anyFollowUp.Count > 0;
	}

	private async Task<Result<PatientHistoryResponse>> BuildResponseAsync(
		string patientId,
		string? patientName,
		IReadOnlyList<Appointment> appointments,
		IReadOnlyList<PatientNote> notes,
		IReadOnlyList<FollowUp> followUps,
		string category,
		DateTime? fromUtc,
		DateTime? toUtc,
		int pageNumber,
		int pageSize)
	{
		var doctorIds = appointments.Select(item => item.DoctorId)
			.Concat(notes.Select(item => item.DoctorId))
			.Concat(followUps.Select(item => item.DoctorId))
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Distinct()
			.ToList();
		var doctors = new Dictionary<string, Doctor>();
		foreach (var doctorId in doctorIds)
		{
			var doctor = await _repos.Doctors.GetById(doctorId, false);
			if (doctor is not null)
			{
				doctors[doctorId] = doctor;
			}
		}

		// A note linked to an appointment is dated by the consultation it
		// describes, not by when the row was written or edited; editing a note
		// must not move it through history.
		var appointmentStartTimes = appointments
			.GroupBy(appointment => appointment.Id)
			.ToDictionary(group => group.Key, group => AsUtc(group.First().StartTime));

		PatientHistoryItemResponse DoctorItem(
			string type,
			DateTime occurredAt,
			string title,
			string? detail,
			string? status,
			string doctorId,
			int sourceId,
			int? appointmentId = null,
			DateTime? dueAtUtc = null,
			bool isPatientVisible = false)
		{
			doctors.TryGetValue(doctorId, out var doctor);
			return new PatientHistoryItemResponse
			{
				Type = type,
				OccurredAtUtc = occurredAt,
				Title = title,
				Detail = detail,
				Status = status,
				AppointmentId = appointmentId,
				DoctorId = doctorId,
				DoctorName = doctor?.FullName,
				DoctorPhotoUrl = doctor?.PhotoUrl,
				DueAtUtc = dueAtUtc,
				IsPatientVisible = isPatientVisible,
				SourceId = sourceId,
			};
		}

		var items = new List<PatientHistoryItemResponse>();
		items.AddRange(appointments.Select(appointment => DoctorItem(
			"appointment",
			AsUtc(appointment.StartTime),
			$"Consultation with Dr. {appointment.DoctorName}",
			appointment.Reason,
			appointment.Status.ToString(),
			appointment.DoctorId,
			appointment.Id,
			appointment.Id)));
		items.AddRange(notes.Select(note => DoctorItem(
			"note",
			note.AppointmentId.HasValue && appointmentStartTimes.TryGetValue(note.AppointmentId.Value, out var noteWhen)
				? noteWhen
				: AsUtc(note.CreatedAtUtc),
			note.Title,
			note.Body,
			null,
			note.DoctorId,
			note.Id,
			note.AppointmentId,
			isPatientVisible: note.IsPatientVisible)));
		items.AddRange(followUps.Select(followUp => DoctorItem(
			"follow_up",
			followUp.DueAtUtc.HasValue ? AsUtc(followUp.DueAtUtc.Value) : AsUtc(followUp.CreatedAtUtc),
			$"Follow-up: {followUp.Reason}",
			null,
			followUp.Status.ToString(),
			followUp.DoctorId,
			followUp.Id,
			followUp.AppointmentId,
			followUp.DueAtUtc.HasValue ? AsUtc(followUp.DueAtUtc.Value) : null,
			followUp.IsPatientVisible)));

		// Filtering happens before ordering and pagination so a page never
		// reorders or drops records relative to the filtered set.
		var filtered = items.AsEnumerable();
		if (category != CategoryAll)
		{
			filtered = filtered.Where(item => item.Type == TypeForCategory(category));
		}

		if (fromUtc.HasValue)
		{
			var from = AsUtc(fromUtc.Value);
			filtered = filtered.Where(item => item.OccurredAtUtc >= from);
		}

		if (toUtc.HasValue)
		{
			var to = AsUtc(toUtc.Value);
			// A date-only "to" arrives as midnight; make it inclusive of that
			// whole UTC day so a record later in the day is not dropped.
			if (to.TimeOfDay == TimeSpan.Zero)
			{
				to = to.AddDays(1).AddTicks(-1);
			}
			filtered = filtered.Where(item => item.OccurredAtUtc <= to);
		}

		var ordered = filtered
			.OrderByDescending(item => item.OccurredAtUtc)
			.ThenBy(item => item.Type, StringComparer.Ordinal)
			.ThenByDescending(item => item.SourceId)
			.ToList();

		pageSize = Math.Clamp(pageSize, 1, 50);
		pageNumber = Math.Max(1, pageNumber);

		return new PatientHistoryResponse
		{
			PatientId = patientId,
			PatientName = patientName,
			TotalCount = ordered.Count,
			PageNumber = pageNumber,
			PageSize = pageSize,
			Items = ordered
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToList(),
		};
	}

	private static string NormalizeCategory(string? category)
	{
		var value = category?.Trim().ToLowerInvariant();
		return value switch
		{
			CategoryAppointments or "appointment" => CategoryAppointments,
			CategoryFollowUps or "followup" or "follow-ups" or "follow_up" => CategoryFollowUps,
			CategoryNotes or "note" => CategoryNotes,
			_ => CategoryAll,
		};
	}

	private static string TypeForCategory(string category) => category switch
	{
		CategoryAppointments => "appointment",
		CategoryFollowUps => "follow_up",
		CategoryNotes => "note",
		_ => string.Empty,
	};

	private static bool IncludesAppointments(string category) =>
		category == CategoryAll || category == CategoryAppointments;

	private static bool IncludesNotes(string category) =>
		category == CategoryAll || category == CategoryNotes;

	private static bool IncludesFollowUps(string category) =>
		category == CategoryAll || category == CategoryFollowUps;

	// SQL Server returns DateTime values without a kind. Every history instant
	// is UTC by contract, so pin the kind before it reaches JSON; otherwise the
	// browser re-reads a bare string in the viewer's own zone and records shift.
	private static DateTime AsUtc(DateTime value) => value.Kind switch
	{
		DateTimeKind.Utc => value,
		DateTimeKind.Local => value.ToUniversalTime(),
		_ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
	};
}
