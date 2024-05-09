using Domain.Entities;

namespace Domain.Repositories;

public interface IDoctorRepository
{
	Task<Doctor?> GetById(string id, bool trackChanges);
	void UpdateDoctor(Doctor doctor);
}
