using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ChatRoomRepository : IChatRoomRepository
{
	private readonly AppDbContext _dbContext;

	public ChatRoomRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	private IQueryable<ChatRoom> Rooms(bool trackChanges) =>
		trackChanges ? _dbContext.ChatRooms : _dbContext.ChatRooms.AsNoTracking();

	private IQueryable<ChatRoomMember> Members(bool trackChanges) =>
		trackChanges ? _dbContext.ChatRoomMembers : _dbContext.ChatRoomMembers.AsNoTracking();

	public Task<ChatRoom?> GetRoomByIdAsync(int roomId, bool trackChanges) =>
		Rooms(trackChanges).FirstOrDefaultAsync(x => x.Id == roomId);

	public async Task<IReadOnlyList<ChatRoom>> GetPublicRoomsAsync(int? beforeRoomId, int limit, bool trackChanges)
	{
		var query = Rooms(trackChanges)
			.Where(x => !x.IsArchived && x.Visibility == ChatRoomVisibility.Public);

		if (beforeRoomId.HasValue)
		{
			query = query.Where(x => x.Id < beforeRoomId.Value);
		}

		return await query
			.OrderByDescending(x => x.Id)
			.Take(limit)
			.ToListAsync();
	}

	public Task<ChatRoomMember?> GetMembershipAsync(int roomId, string userId, bool trackChanges) =>
		Members(trackChanges).FirstOrDefaultAsync(x => x.RoomId == roomId && x.UserId == userId);

	public Task<ChatRoomMember?> GetMembershipByIdAsync(int roomId, int memberId, bool trackChanges) =>
		Members(trackChanges).FirstOrDefaultAsync(x => x.RoomId == roomId && x.Id == memberId);

	public async Task<IReadOnlyList<ChatRoomMember>> GetActiveMembersAsync(int roomId, bool trackChanges)
	{
		return await Members(trackChanges)
			.Where(x => x.RoomId == roomId && x.LeftAtUtc == null)
			.OrderBy(x => x.Id)
			.ToListAsync();
	}

	public async Task<IReadOnlyList<ChatRoomMember>> GetMembershipsAsync(string userId, IReadOnlyCollection<int> roomIds, bool trackChanges)
	{
		if (roomIds.Count == 0)
		{
			return Array.Empty<ChatRoomMember>();
		}

		return await Members(trackChanges)
			.Where(x => x.UserId == userId && roomIds.Contains(x.RoomId))
			.ToListAsync();
	}

	public Task<int> GetActiveMemberCountAsync(int roomId) =>
		Members(false).CountAsync(x => x.RoomId == roomId && x.LeftAtUtc == null);

	public async Task<IReadOnlyDictionary<int, int>> GetActiveMemberCountsAsync(IReadOnlyCollection<int> roomIds)
	{
		if (roomIds.Count == 0)
		{
			return new Dictionary<int, int>();
		}

		var counts = await Members(false)
			.Where(x => roomIds.Contains(x.RoomId) && x.LeftAtUtc == null)
			.GroupBy(x => x.RoomId)
			.Select(group => new { RoomId = group.Key, Count = group.Count() })
			.ToListAsync();

		return counts.ToDictionary(x => x.RoomId, x => x.Count);
	}

	public void CreateRoom(ChatRoom room) => _dbContext.ChatRooms.Add(room);

	public void UpdateRoom(ChatRoom room) => _dbContext.ChatRooms.Update(room);

	public void CreateMembership(ChatRoomMember member) => _dbContext.ChatRoomMembers.Add(member);

	public void UpdateMembership(ChatRoomMember member) => _dbContext.ChatRoomMembers.Update(member);

	public async Task<ChatRoomMember?> CreateMembershipIfAbsentAsync(ChatRoomMember member)
	{
		_dbContext.ChatRoomMembers.Add(member);

		try
		{
			await _dbContext.SaveChangesAsync();
			return null;
		}
		catch (DbUpdateException)
		{
			// A concurrent join may have won the unique (RoomId, UserId) index.
			// Detach the failed insert and return the winner so the retry is
			// idempotent instead of surfacing a 500.
			_dbContext.Entry(member).State = EntityState.Detached;

			var existing = await GetMembershipAsync(member.RoomId, member.UserId, false);
			if (existing is null)
			{
				throw;
			}

			return existing;
		}
	}
}
