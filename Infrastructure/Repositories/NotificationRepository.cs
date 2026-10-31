using System.Text.Json;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shared.RequestParameters;

namespace Infrastructure.Repositories;

public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
{
	public NotificationRepository(AppDbContext dbContext) : base(dbContext)
	{

	}

	public void CreateNotification(Notification notification)
	{
		Create(notification);
	}

	public void UpdateNotification(Notification notification)
	{
		Update(notification);
	}

	public async Task<Notification?> GetById(int notificationId, bool trackChanges)
	{
		return await FindByCondition(n => n.Id == notificationId, trackChanges)
			.FirstOrDefaultAsync();
	}

	public async Task<IEnumerable<Notification>> GetByUserId(string userId, RequestParameters request, bool trackChanges)
	{
		return await FindByCondition(n => n.AppUserId == userId, trackChanges)
			.OrderByDescending(n => n.DateCreated)
			.Paginate(request.PageNumber, request.PageSize)
			.ToListAsync();
	}

	public async Task MarkAllAsRead(string userId)
	{
		await FindByCondition(n => n.AppUserId == userId && !n.IsRead, true)
			.ExecuteUpdateAsync(n => n
				.SetProperty(n => n.IsRead, true)
				.SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow));
	}

	public async Task<int> GetUnreadCountAsync(string userId)
	{
		return await FindByCondition(n => n.AppUserId == userId && !n.IsRead, false)
			.CountAsync();
	}

	public async Task<bool> MarkMultipleAsReadAsync(IEnumerable<int> ids, string userId)
	{
		var idList = ids.ToList();
		if (!idList.Any()) return true;

		await FindByCondition(n => idList.Contains(n.Id) && n.AppUserId == userId && !n.IsRead, true)
			.ExecuteUpdateAsync(n => n
				.SetProperty(n => n.IsRead, true)
				.SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow));

		return true;
	}

	public Task<bool> ExistsByDeduplicationKeyAsync(string userId, string deduplicationKey) =>
		FindByCondition(
				n => n.AppUserId == userId && n.DeduplicationKey == deduplicationKey,
				false)
			.AnyAsync();

	/// <summary>
	/// Creates or refreshes the single notification that represents a room's
	/// unread conversation for one member. The unique filtered index on
	/// (AppUserId, DeduplicationKey) makes the update-then-insert batch safe:
	/// a concurrent insert loses the race and the batch is retried as an
	/// update, so the two messages are counted once each.
	/// </summary>
	public async Task<Notification> UpsertRoomMessageNotificationAsync(
		string userId,
		int roomId,
		string roomName,
		string senderName,
		DateTime now)
	{
		var key = $"room:{roomId}";
		var title = $"New messages in {roomName}";
		var resources = JsonSerializer.Serialize(new Dictionary<string, int> { { "roomId", roomId } });

		// All SET expressions read the pre-update row, so the count expression
		// is repeated instead of relying on SET-clause evaluation order.
		FormattableString upsert = $@"
UPDATE [Notifications]
SET [RelatedCount] = CASE WHEN [IsRead] = 1 THEN 1 ELSE [RelatedCount] + 1 END,
    [Title] = {title},
    [Message] = CASE
        WHEN (CASE WHEN [IsRead] = 1 THEN 1 ELSE [RelatedCount] + 1 END) = 1
            THEN N'1 new message in ' + {roomName}
        ELSE CONVERT(nvarchar(12), CASE WHEN [IsRead] = 1 THEN 1 ELSE [RelatedCount] + 1 END)
            + N' new messages in ' + {roomName}
    END,
    [NotifierUserName] = {senderName},
    [NotifierPhotoUrl] = NULL,
    [DateCreated] = {now},
    [IsRead] = 0,
    [ReadAt] = NULL,
    [Resources] = {resources},
    [Type] = {nameof(NotificationType.RoomMessage)},
    [RelatedResourceId] = {roomId}
WHERE [AppUserId] = {userId} AND [DeduplicationKey] = {key};

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO [Notifications]
        ([AppUserId], [Title], [Message], [DateCreated], [Resources], [Type],
         [IsRead], [RelatedResourceId], [DeduplicationKey], [NotifierUserName],
         [NotifierPhotoUrl], [RelatedCount])
    VALUES
        ({userId}, {title}, N'1 new message in ' + {roomName}, {now}, {resources},
         {nameof(NotificationType.RoomMessage)}, 0, {roomId}, {key}, {senderName},
         NULL, 1);
END;";

		for (var attempt = 0; attempt < 2; attempt++)
		{
			try
			{
				await _dbContext.Database.ExecuteSqlInterpolatedAsync(upsert);
				break;
			}
			catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
			{
				if (attempt == 1) throw;
			}
			catch (SqlException ex) when (ex.Number is 2601 or 2627)
			{
				if (attempt == 1) throw;
			}
		}

		return await FindByCondition(
				n => n.AppUserId == userId && n.DeduplicationKey == key,
				false)
			.AsNoTracking()
			.FirstAsync();
	}

	public async Task<IReadOnlyList<Notification>> MarkRoomNotificationsAsReadAsync(
		string userId,
		int roomId,
		DateTimeOffset readAt)
	{
		// Only the notification row is marked read. The member's message read
		// position is owned by the chat read endpoint and is not touched here.
		var notifications = await FindByCondition(
				n => n.AppUserId == userId &&
				     n.Type == nameof(NotificationType.RoomMessage) &&
				     n.RelatedResourceId == roomId &&
				     !n.IsRead,
				true)
			.ToListAsync();

		foreach (var notification in notifications)
		{
			notification.IsRead = true;
			notification.ReadAt = readAt;
		}

		return notifications;
	}
}
