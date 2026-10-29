using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DoctorWorkspaceRepository : IDoctorWorkspaceRepository
{
	private readonly AppDbContext _dbContext;

	public DoctorWorkspaceRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<IReadOnlyList<PatientNote>> GetNotesAsync(
		string doctorId,
		string? patientId,
		int limit,
		bool trackChanges)
	{
		var query = Notes(trackChanges).Where(x => x.DoctorId == doctorId);

		if (!string.IsNullOrWhiteSpace(patientId))
		{
			query = query.Where(x => x.PatientId == patientId);
		}

		return await query
			.OrderByDescending(x => x.CreatedAtUtc)
			.ThenByDescending(x => x.Id)
			.Take(limit)
			.ToListAsync();
	}

	public Task<PatientNote?> GetNoteAsync(int noteId, string doctorId, bool trackChanges) =>
		Notes(trackChanges).FirstOrDefaultAsync(x => x.Id == noteId && x.DoctorId == doctorId);

	public void CreateNote(PatientNote note) => _dbContext.PatientNotes.Add(note);

	public void UpdateNote(PatientNote note) => _dbContext.PatientNotes.Update(note);

	public async Task<IReadOnlyList<PatientNote>> GetRecentNotesAsync(string doctorId, DateTime sinceUtc, int take)
	{
		return await Notes(false)
			.Where(x => x.DoctorId == doctorId && x.CreatedAtUtc >= sinceUtc)
			.OrderByDescending(x => x.CreatedAtUtc)
			.Take(take)
			.ToListAsync();
	}

	public async Task<IReadOnlyList<FollowUp>> GetFollowUpsAsync(
		string doctorId,
		FollowUpStatus? status,
		int limit,
		bool trackChanges)
	{
		var query = FollowUps(trackChanges).Where(x => x.DoctorId == doctorId);

		if (status.HasValue)
		{
			query = query.Where(x => x.Status == status.Value);
		}

		return await query
			.OrderBy(x => x.Status == FollowUpStatus.Open ? 0 : 1)
			.ThenBy(x => x.DueAtUtc == null)
			.ThenBy(x => x.DueAtUtc)
			.ThenByDescending(x => x.Id)
			.Take(limit)
			.ToListAsync();
	}

	public Task<FollowUp?> GetFollowUpAsync(int followUpId, string doctorId, bool trackChanges) =>
		FollowUps(trackChanges).FirstOrDefaultAsync(x => x.Id == followUpId && x.DoctorId == doctorId);

	public void CreateFollowUp(FollowUp followUp) => _dbContext.FollowUps.Add(followUp);

	public void UpdateFollowUp(FollowUp followUp) => _dbContext.FollowUps.Update(followUp);

	public Task<int> CountOpenFollowUpsAsync(string doctorId) =>
		FollowUps(false).CountAsync(x => x.DoctorId == doctorId && x.Status == FollowUpStatus.Open);

	public async Task<IReadOnlyList<FollowUp>> GetDueOpenFollowUpsAsync(
		string doctorId,
		DateTime nowUtc,
		int take)
	{
		return await FollowUps(false)
			.Where(x => x.DoctorId == doctorId &&
				x.Status == FollowUpStatus.Open &&
				x.DueAtUtc != null &&
				x.DueAtUtc <= nowUtc)
			.OrderBy(x => x.DueAtUtc)
			.Take(take)
			.ToListAsync();
	}

	private IQueryable<PatientNote> Notes(bool trackChanges) =>
		trackChanges ? _dbContext.PatientNotes : _dbContext.PatientNotes.AsNoTracking();

	private IQueryable<FollowUp> FollowUps(bool trackChanges) =>
		trackChanges ? _dbContext.FollowUps : _dbContext.FollowUps.AsNoTracking();
}
