using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

public interface IDoctorWorkspaceRepository
{
	Task<IReadOnlyList<PatientNote>> GetNotesAsync(
		string doctorId,
		string? patientId,
		int limit,
		bool trackChanges);

	Task<PatientNote?> GetNoteAsync(int noteId, string doctorId, bool trackChanges);

	void CreateNote(PatientNote note);

	void UpdateNote(PatientNote note);

	Task<IReadOnlyList<PatientNote>> GetRecentNotesAsync(string doctorId, DateTime sinceUtc, int take);

	Task<IReadOnlyList<PatientNote>> GetVisibleNotesForPatientAsync(string patientId, int take);
	Task<IReadOnlyList<PatientNote>> GetNotesForDoctorPatientAsync(string doctorId, string patientId, int take);

	Task<IReadOnlyList<FollowUp>> GetFollowUpsAsync(
		string doctorId,
		FollowUpStatus? status,
		int limit,
		bool trackChanges);

	Task<FollowUp?> GetFollowUpAsync(int followUpId, string doctorId, bool trackChanges);

	void CreateFollowUp(FollowUp followUp);

	void UpdateFollowUp(FollowUp followUp);

	Task<int> CountOpenFollowUpsAsync(string doctorId);

	/// <summary>Open follow-ups that are due or overdue, oldest due date first.</summary>
	Task<IReadOnlyList<FollowUp>> GetDueOpenFollowUpsAsync(string doctorId, DateTime nowUtc, int take);

	Task<IReadOnlyList<FollowUp>> GetVisibleFollowUpsForPatientAsync(string patientId, int take);
	Task<IReadOnlyList<FollowUp>> GetFollowUpsForDoctorPatientAsync(string doctorId, string patientId, int take);
}
