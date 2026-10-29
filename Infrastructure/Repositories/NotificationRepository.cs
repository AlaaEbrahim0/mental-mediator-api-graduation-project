using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
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
}
