using Domain.Entities;

namespace Domain.Repositories;

public interface IWeeklyScheduleRepository
{
	Task<WeeklySchedule?> GetById(string doctorId, int scheduleId, bool trackChanges);
	void CreateWeeklySchedule(WeeklySchedule weeklySchedule);
	void UpdateWeeklySchedule(WeeklySchedule weeklySchedule);
}
