using Domain.Entities;

namespace Domain.Repositories;

public interface IDoctorScheduleBlockRepository
{
	Task<IEnumerable<DoctorScheduleBlock>> GetByDoctorId(string doctorId, bool trackChanges);
	Task<DoctorScheduleBlock?> GetById(int id, bool trackChanges);
	void CreateScheduleBlock(DoctorScheduleBlock block);
	void DeleteScheduleBlock(DoctorScheduleBlock block);
}