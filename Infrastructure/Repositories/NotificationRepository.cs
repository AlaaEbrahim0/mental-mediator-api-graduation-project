using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;

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

	public void DeleteNotification(Notification notification)
	{
		Delete(notification);
	}


	public async Task<Notification?> GetById(int notificationId, bool trackChanges)
	{
		return await FindByCondition(n => n.Id == notificationId, trackChanges)
			.OrderByDescending(n => n.DateCreated)
			.FirstOrDefaultAsync();
	}

	public async Task<IEnumerable<Notification>> GetByUserId(string userId, RequestParameters request, bool trackChanges)
	{
		return await FindByCondition(n => n.AppUserId == userId, trackChanges)
			.OrderByDescending(n => n.DateCreated)
			.Paginate(request.PageNumber, request.PageSize)
			.ToListAsync();
	}

	public void UpdateNotification(Notification notification)
	{
		Update(notification);
	}
}
