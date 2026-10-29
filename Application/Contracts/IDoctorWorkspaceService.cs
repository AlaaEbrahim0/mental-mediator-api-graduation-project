using Application.Dtos.DoctorWorkspaceDtos;
using Domain.Enums;
using Shared;

namespace Application.Contracts;

/// <summary>
/// The doctor's own workspace: dashboard aggregates, a merged activity feed,
/// and private notes/follow-ups. Every method is scoped to the signed-in
/// doctor; there is no route that accepts another doctor's id.
/// </summary>
public interface IDoctorWorkspaceService
{
	Task<Result<DoctorDashboardResponse>> GetDashboardAsync();

	Task<Result<IReadOnlyList<DoctorActivityItemResponse>>> GetActivityAsync(int limit);

	Task<Result<IReadOnlyList<PatientNoteResponse>>> GetNotesAsync(string? patientId, int limit);

	Task<Result<PatientNoteResponse>> CreateNoteAsync(CreatePatientNoteRequest request);

	Task<Result<PatientNoteResponse>> UpdateNoteAsync(int noteId, UpdatePatientNoteRequest request);

	Task<Result<IReadOnlyList<FollowUpResponse>>> GetFollowUpsAsync(FollowUpStatus? status, int limit);

	Task<Result<FollowUpResponse>> CreateFollowUpAsync(CreateFollowUpRequest request);

	Task<Result<FollowUpResponse>> UpdateFollowUpAsync(int followUpId, UpdateFollowUpRequest request);
}
