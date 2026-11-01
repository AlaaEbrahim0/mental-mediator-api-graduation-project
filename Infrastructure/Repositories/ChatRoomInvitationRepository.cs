using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ChatRoomInvitationRepository : IChatRoomInvitationRepository
{
	private readonly AppDbContext _dbContext;

	public ChatRoomInvitationRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	private IQueryable<ChatRoomInvitation> Query(bool trackChanges) =>
		trackChanges ? _dbContext.ChatRoomInvitations : _dbContext.ChatRoomInvitations.AsNoTracking();

	public void Create(ChatRoomInvitation invitation) => _dbContext.ChatRoomInvitations.Add(invitation);

	public void Update(ChatRoomInvitation invitation) => _dbContext.ChatRoomInvitations.Update(invitation);

	public Task<ChatRoomInvitation?> GetByIdAsync(int id, bool trackChanges) =>
		Query(trackChanges).FirstOrDefaultAsync(x => x.Id == id);

	public Task<ChatRoomInvitation?> GetPendingAsync(int roomId, string userId, bool trackChanges) =>
		Query(trackChanges).FirstOrDefaultAsync(x =>
			x.RoomId == roomId && x.InvitedUserId == userId && x.Status == InvitationStatus.Pending);

	public async Task<IReadOnlyCollection<string>> GetPendingUserIdsAsync(int roomId) =>
		await _dbContext.ChatRoomInvitations
			.Where(x => x.RoomId == roomId && x.Status == InvitationStatus.Pending)
			.Select(x => x.InvitedUserId)
			.ToListAsync();

	public async Task<IReadOnlyList<ChatRoomInvitation>> GetPendingForRoomAsync(int roomId, bool trackChanges) =>
		await Query(trackChanges)
			.Where(x => x.RoomId == roomId && x.Status == InvitationStatus.Pending)
			.Include(x => x.InvitedUser)
			.OrderBy(x => x.CreatedAtUtc)
			.ToListAsync();

	public async Task<IReadOnlyList<ChatRoomInvitation>> GetPendingForUserAsync(string userId, bool trackChanges) =>
		await Query(trackChanges)
			.Where(x => x.InvitedUserId == userId && x.Status == InvitationStatus.Pending)
			.Include(x => x.Room)
			.Include(x => x.InvitedBy)
			.OrderByDescending(x => x.CreatedAtUtc)
			.ToListAsync();

	public async Task<IReadOnlyList<BaseUser>> SearchInviteCandidatesAsync(
		string? search,
		IReadOnlyCollection<string> excludeUserIds,
		int take)
	{
		var query = _dbContext.Set<BaseUser>().AsNoTracking();

		if (excludeUserIds.Count > 0)
		{
			query = query.Where(user => !excludeUserIds.Contains(user.Id));
		}

		if (!string.IsNullOrWhiteSpace(search))
		{
			var needle = search.Trim();
			query = query.Where(user =>
				user.FirstName.Contains(needle) ||
				user.LastName.Contains(needle) ||
				(user.FirstName + " " + user.LastName).Contains(needle));
		}

		return await query
			.OrderBy(user => user.FirstName)
			.ThenBy(user => user.LastName)
			.Take(take)
			.ToListAsync();
	}
}
