using Domain.Entities;

namespace Domain.Repositories;

public interface IDoctorScheduleRepository
{

	Task<WeeklySchedule> GetSchedule(string doctorId, bool trackChanges);

	Task<DoctorScheduleWeekDay?> GetScheduleWeekDay(string doctorId, DayOfWeek dayOfWeek, bool trackChanges);

	Task CreateDoctorWeeklySchedule(string doctorId, WeeklySchedule schedule);

	void DeleteDoctorSchedule(WeeklySchedule schedule);

	void CreateScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule);
	void UpdateScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule);
	void DeleteScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule);
}
