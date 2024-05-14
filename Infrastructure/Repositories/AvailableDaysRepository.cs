using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AvailableDaysRepository : RepositoryBase<AvailableDays>,
	IAvailableDayRepository
{
	public AvailableDaysRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public async Task<AvailableDays?> GetAvailableDay(int scheduleId, int availableDayId, bool trackChanges)
	{
		return await FindByCondition(w => w.WeeklyScheduleId == scheduleId &&
			w.Id == availableDayId, trackChanges)
			.SingleOrDefaultAsync();
	}

	public async Task<List<AvailableDays>> GetAvailableDays(int scheduleId, bool trackChanges)
	{
		return await FindByCondition(w => w.WeeklyScheduleId == scheduleId, trackChanges)
			.ToListAsync();
	}

	public void UpdateAvailableDay(AvailableDays AvailableDays)
	{
		Update(AvailableDays);
	}

	public void DeleteAvailableDay(AvailableDays AvailableDays)
	{
		Delete(AvailableDays);
	}

	public void CreateAvailableDay(AvailableDays availableDays)
	{
		Create(availableDays);
	}


}
