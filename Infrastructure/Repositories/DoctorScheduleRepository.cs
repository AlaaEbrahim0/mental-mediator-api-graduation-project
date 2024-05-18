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

	public async Task CreateDoctorWeeklySchedule(string doctorId, List<DoctorScheduleWeekDay> schedule)
	{
		await _dbContext
			.AddRangeAsync(schedule);
	}

	public async Task DeleteDoctorSchedule(string doctorId)
	{
		var dayweeks = await this.GetSchedule(doctorId, true);
		_dbContext.ScheduleWeekDays.RemoveRange(dayweeks);
	}

	public void CreateScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule)
	{
		Create(weeklySchedule);
	}

	public void DeleteScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule)
	{
		Delete(weeklySchedule);
	}

	public async Task<List<DoctorScheduleWeekDay>> GetSchedule(string doctorId, bool trackChanges)
	{
		return await FindByCondition(x => x.DoctorId == doctorId, trackChanges)
			.OrderBy(x => x.DayOfWeek)
			.ToListAsync();

	}

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
