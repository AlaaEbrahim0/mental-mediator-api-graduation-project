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

	public async Task<Result<PatientHistoryResponse>> GetMyHistoryAsync(int pageNumber, int pageSize)
	{
		var patientId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(patientId))
		{
			return HistoryErrors.NotFound();
		}

		var appointments = await _repos.Appointments.GetHistoryForPatientAsync(
			patientId,
			doctorId: null,
			MaxItemsPerSource,
			false);
		var notes = await _repos.DoctorWorkspace.GetVisibleNotesForPatientAsync(
			patientId,
			MaxItemsPerSource);
		var followUps = await _repos.DoctorWorkspace.GetVisibleFollowUpsForPatientAsync(
			patientId,
			MaxItemsPerSource);

		var names = await _patients.GetDisplayNamesAsync(new[] { patientId });
		return await BuildResponseAsync(
			patientId,
			names.GetValueOrDefault(patientId),
			appointments,
			notes,
			followUps,
			pageNumber,
			pageSize);
	}

	public async Task<Result<PatientHistoryResponse>> GetPatientHistoryAsync(
		string patientId,
		int pageNumber,
		int pageSize)
	{
		var doctorId = _claims.GetUserId();
		if (string.IsNullOrWhiteSpace(doctorId) ||
			string.IsNullOrWhiteSpace(patientId))
		{
			return HistoryErrors.NotFound();
		}

		var appointments = await _repos.Appointments.GetHistoryForPatientAsync(
			patientId,
			doctorId,
			MaxItemsPerSource,
			false);
		var notes = await _repos.DoctorWorkspace.GetNotesForDoctorPatientAsync(
			doctorId,
			patientId,
			MaxItemsPerSource);
		var followUps = await _repos.DoctorWorkspace.GetFollowUpsForDoctorPatientAsync(
			doctorId,
			patientId,
			MaxItemsPerSource);

		// Having no record of any kind is not proof of a care relationship.
		if (appointments.Count == 0 && notes.Count == 0 && followUps.Count == 0)
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
			pageNumber,
			pageSize);
	}

	private async Task<Result<PatientHistoryResponse>> BuildResponseAsync(
		string patientId,
		string? patientName,
		IReadOnlyList<Appointment> appointments,
		IReadOnlyList<PatientNote> notes,
		IReadOnlyList<FollowUp> followUps,
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

		PatientHistoryItemResponse DoctorItem(
			string type,
			DateTime occurredAt,
			string title,
			string? detail,
			string? status,
			string doctorId,
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
			};
		}

		var items = new List<PatientHistoryItemResponse>();
		items.AddRange(appointments.Select(appointment => DoctorItem(
			"appointment",
			appointment.StartTime,
			$"Consultation with Dr. {appointment.DoctorName}",
			appointment.Reason,
			appointment.Status.ToString(),
			appointment.DoctorId,
			appointment.Id)));
		items.AddRange(notes.Select(note => DoctorItem(
			"note",
			note.UpdatedAtUtc ?? note.CreatedAtUtc,
			note.Title,
			note.Body,
			null,
			note.DoctorId,
			note.AppointmentId,
			isPatientVisible: note.IsPatientVisible)));
		items.AddRange(followUps.Select(followUp => DoctorItem(
			"follow_up",
			followUp.DueAtUtc ?? followUp.CreatedAtUtc,
			$"Follow-up: {followUp.Reason}",
			null,
			followUp.Status.ToString(),
			followUp.DoctorId,
			followUp.AppointmentId,
			followUp.DueAtUtc,
			followUp.IsPatientVisible)));

		var ordered = items
			.OrderByDescending(item => item.OccurredAtUtc)
			.ThenBy(item => item.Type, StringComparer.Ordinal)
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
}
