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

	public async Task<IReadOnlyList<ChatRoom>> GetListableRoomsAsync(int? beforeRoomId, int limit, bool trackChanges)
	{
		// Private rooms are listed on purpose: membership gates the conversation
		// and the description in the service layer, not the directory entry.
		var query = Rooms(trackChanges)
			.Where(x => !x.IsArchived);

		if (beforeRoomId.HasValue)
		{
			query = query.Where(x => x.Id < beforeRoomId.Value);
		}

		return await query
			.OrderByDescending(x => x.Id)
			.Take(limit)
			.ToListAsync();
	}

	public async Task<(IReadOnlyList<ChatRoom> Rooms, int TotalCount)> BrowseRoomsAsync(RoomBrowseQuery query)
	{
		var filtered = Rooms(false).Where(x => !x.IsArchived);

		filtered = query.Tab switch
		{
			RoomBrowseTab.Active => filtered.Where(x =>
				x.LastMessageAtUtc != null && x.LastMessageAtUtc >= query.ActiveSinceUtc),
			RoomBrowseTab.Private => filtered.Where(x => x.Visibility == ChatRoomVisibility.Private),
			RoomBrowseTab.Scheduled => filtered.Where(x =>
				x.ScheduledStartAtUtc != null && x.ScheduledStartAtUtc >= query.NowUtc),
			RoomBrowseTab.PatientCreated => filtered.Where(x =>
				x.Members.Any(member =>
					member.Role == ChatRoomRole.Owner && member.LeftAtUtc == null && !member.IsDoctor)),
			_ => filtered
		};

		var search = query.Search?.Trim();
		if (!string.IsNullOrEmpty(search))
		{
			var needle = search.ToLower();

			// Lower-cased explicitly rather than relying on the database collation:
			// SQL Server is case-insensitive by default here, but the predicate has
			// to behave the same way against the in-memory provider used in tests.
			// A private room's description is member-only content, so it is not
			// searched at all — matching on it would leak through the hit list.
			filtered = filtered.Where(x =>
				x.Name.ToLower().Contains(needle) ||
				(x.Visibility == ChatRoomVisibility.Public && x.Description.ToLower().Contains(needle)));
		}

		var totalCount = await filtered.CountAsync();

		IOrderedQueryable<ChatRoom> ordered = query.Sort switch
		{
			RoomBrowseSort.Name => filtered.OrderBy(x => x.Name).ThenByDescending(x => x.Id),
			RoomBrowseSort.Members => filtered
				.OrderByDescending(x => x.Members.Count(member => member.LeftAtUtc == null))
				.ThenByDescending(x => x.Id),
			RoomBrowseSort.Soonest => filtered
				.OrderBy(x => x.ScheduledStartAtUtc == null)
				.ThenBy(x => x.ScheduledStartAtUtc)
				.ThenByDescending(x => x.Id),
			_ => filtered
				.OrderByDescending(x => x.LastMessageAtUtc == null)
				.ThenByDescending(x => x.LastMessageAtUtc)
				.ThenByDescending(x => x.Id)
		};

		var rooms = await ordered
			.Skip(query.Skip)
			.Take(query.Take)
			.ToListAsync();

		return (rooms, totalCount);
	}

	public async Task<IReadOnlyList<ChatRoom>> GetRoomsCreatedAsync(string userId, bool includeArchived, int limit)
	{
		var query = Rooms(false).Where(x => x.CreatedById == userId);

		if (!includeArchived)
		{
			query = query.Where(x => !x.IsArchived);
		}

		return await query
			.OrderByDescending(x => x.Id)
			.Take(limit)
			.ToListAsync();
	}

	public Task<int> CountRoomsCreatedAsync(string userId, bool includeArchived)
	{
		var query = Rooms(false).Where(x => x.CreatedById == userId);

		return includeArchived
			? query.CountAsync()
			: query.CountAsync(x => !x.IsArchived);
	}

	public async Task<IReadOnlyList<ChatRoom>> GetUpcomingScheduledAsync(
		IReadOnlyCollection<int> roomIds,
		DateTime fromUtc,
		int limit)
	{
		if (roomIds.Count == 0)
		{
			return Array.Empty<ChatRoom>();
		}

		return await Rooms(false)
			.Where(x => roomIds.Contains(x.Id) && !x.IsArchived && x.ScheduledStartAtUtc != null && x.ScheduledStartAtUtc >= fromUtc)
			.OrderBy(x => x.ScheduledStartAtUtc)
			.Take(limit)
			.ToListAsync();
	}

	public Task<ChatRoomMember?> GetOwnerAsync(int roomId, bool trackChanges) =>
		Members(trackChanges).FirstOrDefaultAsync(x =>
			x.RoomId == roomId && x.Role == ChatRoomRole.Owner && x.LeftAtUtc == null);

	public async Task<IReadOnlyDictionary<int, ChatRoomMember>> GetOwnersByRoomIdsAsync(IReadOnlyCollection<int> roomIds)
	{
		if (roomIds.Count == 0)
		{
			return new Dictionary<int, ChatRoomMember>();
		}

		var owners = await Members(false)
			.Where(x => roomIds.Contains(x.RoomId) && x.Role == ChatRoomRole.Owner && x.LeftAtUtc == null)
			.ToListAsync();

		// GroupBy-then-first rather than ToDictionary: two owner rows for one room
		// would be a data bug, and it must not throw inside a directory request.
		return owners
			.GroupBy(x => x.RoomId)
			.ToDictionary(group => group.Key, group => group.First());
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

	public async Task<IReadOnlyList<ChatRoomMember>> GetRecentJoinsAsync(
		IReadOnlyCollection<int> roomIds,
		DateTime sinceUtc,
		int take)
	{
		if (roomIds.Count == 0)
		{
			return Array.Empty<ChatRoomMember>();
		}

		return await Members(false)
			.Where(x => roomIds.Contains(x.RoomId) && x.LeftAtUtc == null && x.JoinedAtUtc >= sinceUtc)
			.OrderByDescending(x => x.JoinedAtUtc)
			.Take(take)
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
