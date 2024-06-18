using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DoctorScheduleRepository : RepositoryBase<DoctorScheduleWeekDay>, IDoctorScheduleRepository
{
	public DoctorScheduleRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public async Task CreateDoctorWeeklySchedule(string doctorId, WeeklySchedule weeklySchedule)
	{
		await _dbContext.AddRangeAsync(weeklySchedule.WeekDays);
	}

	public void DeleteDoctorSchedule(WeeklySchedule schedule)
	{
		_dbContext.ScheduleWeekDays.RemoveRange(schedule.WeekDays);
	}

	public void CreateScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule)
	{
		Create(weeklySchedule);
	}

	public void DeleteScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule)
	{
		Delete(weeklySchedule);
	}

	public async Task<WeeklySchedule> GetSchedule(string doctorId, bool trackChanges)
	{
		var weekDays = await FindByCondition(x => x.DoctorId == doctorId, trackChanges)
			.OrderBy(x => x.DayOfWeek)
			.ToListAsync();

		return new WeeklySchedule { WeekDays = weekDays };
	}

	//public async Task<List<WeeklySchedule>> GetSchedules(bool trackChanges)
	//{
	//	var weekDays = await FindAll(trackChanges)
	//		.OrderBy(x => x.DayOfWeek)
	//		.ToListAsync();

	//}



	public async Task<DoctorScheduleWeekDay?> GetScheduleWeekDay(string doctorId, DayOfWeek dayOfWeek, bool trackChanges)
	{
		return await
			FindByCondition(
				x => x.DoctorId == doctorId &&
				x.DayOfWeek == dayOfWeek,
				trackChanges)
			.SingleOrDefaultAsync();

	}

	public void UpdateScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule)
	{
		Update(weeklySchedule);
	}

}
