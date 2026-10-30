using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
	private readonly AppDbContext _dbContext;

	public ChatMessageRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	private IQueryable<ChatMessage> Messages(bool trackChanges) =>
		trackChanges ? _dbContext.ChatMessages : _dbContext.ChatMessages.AsNoTracking();

	public Task<ChatMessage?> GetMessageByIdAsync(long messageId, bool trackChanges) =>
		Messages(trackChanges).FirstOrDefaultAsync(x => x.Id == messageId);

	public Task<ChatMessage?> GetByClientMessageIdAsync(string senderId, Guid clientMessageId, bool trackChanges) =>
		Messages(trackChanges).FirstOrDefaultAsync(x => x.SenderId == senderId && x.ClientMessageId == clientMessageId);

	public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(int roomId, long? beforeId, long? afterId, int limit, bool trackChanges)
	{
		var query = Messages(trackChanges).Where(x => x.RoomId == roomId);

		if (afterId.HasValue)
		{
			return await query
				.Where(x => x.Id > afterId.Value)
				.OrderBy(x => x.Id)
				.Take(limit)
				.ToListAsync();
		}

		if (beforeId.HasValue)
		{
			var page = await query
				.Where(x => x.Id < beforeId.Value)
				.OrderByDescending(x => x.Id)
				.Take(limit)
				.ToListAsync();

			page.Reverse();
			return page;
		}

		var latest = await query
			.OrderByDescending(x => x.Id)
			.Take(limit)
			.ToListAsync();

		latest.Reverse();
		return latest;
	}

	public Task<ChatMessage?> GetLatestMessageAsync(int roomId, bool trackChanges) =>
		Messages(trackChanges)
			.Where(x => x.RoomId == roomId && !x.IsDeleted)
			.OrderByDescending(x => x.Id)
			.FirstOrDefaultAsync();

	public async Task<IReadOnlyDictionary<int, ChatMessage>> GetLatestMessagesAsync(IReadOnlyCollection<int> roomIds, bool trackChanges)
	{
		if (roomIds.Count == 0)
		{
			return new Dictionary<int, ChatMessage>();
		}

		var latestIds = await Messages(false)
			.Where(x => roomIds.Contains(x.RoomId) && !x.IsDeleted)
			.GroupBy(x => x.RoomId)
			.Select(group => group.Max(x => x.Id))
			.ToListAsync();

		if (latestIds.Count == 0)
		{
			return new Dictionary<int, ChatMessage>();
		}

		var messages = await Messages(trackChanges)
			.Where(x => latestIds.Contains(x.Id))
			.ToListAsync();

		return messages.ToDictionary(x => x.RoomId);
	}

	public async Task<IReadOnlyDictionary<int, int>> GetUnreadCountsAsync(string userId, IReadOnlyCollection<int> roomIds)
	{
		if (roomIds.Count == 0)
		{
			return new Dictionary<int, int>();
		}

		// One query instead of one per room: the membership row itself carries
		// the read threshold, so message and member are joined on RoomId.
		var counts = await _dbContext.ChatMessages
			.AsNoTracking()
			.Where(message =>
				roomIds.Contains(message.RoomId) &&
				!message.IsDeleted &&
				message.SenderId != userId)
			.Join(
				_dbContext.ChatRoomMembers
					.AsNoTracking()
					.Where(member => member.UserId == userId && member.LeftAtUtc == null),
				message => message.RoomId,
				member => member.RoomId,
				(message, member) => new { Message = message, Member = member })
			.Where(pair => pair.Message.SentAtUtc > (pair.Member.LastReadAtUtc ?? pair.Member.JoinedAtUtc))
			.GroupBy(pair => pair.Message.RoomId)
			.Select(group => new { RoomId = group.Key, Count = group.Count() })
			.ToListAsync();

		return counts.ToDictionary(x => x.RoomId, x => x.Count);
	}

	public async Task<ChatMessage?> CreateMessageIfAbsentAsync(ChatMessage message)
	{
		_dbContext.ChatMessages.Add(message);

		try
		{
			await _dbContext.SaveChangesAsync();
			return null;
		}
		catch (DbUpdateException)
		{
			// A concurrent request may have won the unique (SenderId, ClientMessageId)
			// index. Detach the failed insert and return the winner so the caller can
			// compare it with the request before treating the retry as idempotent.
			_dbContext.Entry(message).State = EntityState.Detached;

			var existing = await GetByClientMessageIdAsync(message.SenderId, message.ClientMessageId, false);
			if (existing is null)
			{
				throw;
			}

			return existing;
		}
	}

	public void CreateMessage(ChatMessage message) => _dbContext.ChatMessages.Add(message);

	public void UpdateMessage(ChatMessage message) => _dbContext.ChatMessages.Update(message);
}
