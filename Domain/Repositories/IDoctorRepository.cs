using Domain.Entities;
using Shared;

namespace Domain.Repositories;

public interface IDoctorRepository
{
	Task<IEnumerable<Doctor>> GetAll(RequestParameters requestParameters, bool trackChanges);
	Task<Doctor?> GetById(string id, bool trackChanges);
	void UpdateDoctor(Doctor doctor);
}
