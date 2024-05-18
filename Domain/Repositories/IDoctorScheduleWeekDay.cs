using Domain.Entities;

namespace Domain.Repositories;

public interface IDoctorScheduleRepository
{

	Task<List<DoctorScheduleWeekDay>> GetSchedule(string doctorId, bool trackChanges);

	Task<DoctorScheduleWeekDay?> GetScheduleWeekDay(string doctorId, DayOfWeek dayOfWeek, bool trackChanges);

	Task CreateDoctorWeeklySchedule(string doctorId, List<DoctorScheduleWeekDay> schedule);

	Task DeleteDoctorSchedule(string doctorId);

	void CreateScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule);
	void UpdateScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule);
	void DeleteScheduleWeekDay(DoctorScheduleWeekDay weeklySchedule);
}
