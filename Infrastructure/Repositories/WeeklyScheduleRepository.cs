using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class WeeklyScheduleRepository : RepositoryBase<WeeklySchedule>, IWeeklyScheduleRepository
{
	public WeeklyScheduleRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public void CreateWeeklySchedule(WeeklySchedule weeklySchedule)
	{
		Create(weeklySchedule);
	}


	public async Task<WeeklySchedule?> GetById(string doctorId, int scheduleId, bool trackChanges)
	{
		return await
			FindByCondition(x => x.DoctorId!.Equals(doctorId), trackChanges)
			.Include(x => x.AvailableDays)
			.SingleOrDefaultAsync();
	}

	public void UpdateWeeklySchedule(WeeklySchedule weeklySchedule)
	{
		Update(weeklySchedule);
	}

	public void DeleteWeeklySchecule(WeeklySchedule weeklySchedule)
	{
		Delete(weeklySchedule);
	}
}
