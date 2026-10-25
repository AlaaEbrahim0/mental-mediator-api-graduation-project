using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ChatMessageReportRepository : IChatMessageReportRepository
{
	private readonly AppDbContext _dbContext;

	public ChatMessageReportRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	private IQueryable<ChatMessageReport> Reports(bool trackChanges) =>
		trackChanges ? _dbContext.ChatMessageReports : _dbContext.ChatMessageReports.AsNoTracking();

	public Task<ChatMessageReport?> GetReportByIdAsync(int reportId, bool trackChanges) =>
		Reports(trackChanges)
			.Include(x => x.Message)
			.FirstOrDefaultAsync(x => x.Id == reportId);

	public Task<ChatMessageReport?> GetReportAsync(long messageId, string reporterId, bool trackChanges) =>
		Reports(trackChanges).FirstOrDefaultAsync(x => x.MessageId == messageId && x.ReporterId == reporterId);

	public async Task<IReadOnlyList<ChatMessageReport>> GetReportsForRoomAsync(int roomId, ChatReportStatus? status, int limit, bool trackChanges)
	{
		var query = Reports(trackChanges)
			.Include(x => x.Message)
			.Where(x => x.Message.RoomId == roomId);

		if (status.HasValue)
		{
			query = query.Where(x => x.Status == status.Value);
		}

		return await query
			.OrderByDescending(x => x.CreatedAtUtc)
			.Take(limit)
			.ToListAsync();
	}

	public void CreateReport(ChatMessageReport report) => _dbContext.ChatMessageReports.Add(report);

	public void UpdateReport(ChatMessageReport report) => _dbContext.ChatMessageReports.Update(report);

	public async Task<ChatMessageReport?> CreateReportIfAbsentAsync(ChatMessageReport report)
	{
		_dbContext.ChatMessageReports.Add(report);

		try
		{
			await _dbContext.SaveChangesAsync();
			return null;
		}
		catch (DbUpdateException)
		{
			// A concurrent request may have won the unique
			// (MessageId, ReporterId) index.
			_dbContext.Entry(report).State = EntityState.Detached;

			var existing = await GetReportAsync(report.MessageId, report.ReporterId, false);
			if (existing is null)
			{
				throw;
			}

			return existing;
		}
	}
}
