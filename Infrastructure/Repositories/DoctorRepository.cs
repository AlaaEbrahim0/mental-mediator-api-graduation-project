using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Infrastructure.Repositories;

public class DoctorRepository : RepositoryBase<Doctor>, IDoctorRepository
{
	public DoctorRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public async Task<IEnumerable<Doctor>> GetAll(RequestParameters requestParameters, bool trackChanges)
	{
		var doctors = await FindAll(trackChanges)
			.Paginate(requestParameters.PageNumber, requestParameters.PageSize)
			.ToListAsync();

		return doctors;
	}

	public async Task<Doctor?> GetById(string id, bool trackChanges)
	{
		var doctor = await FindByCondition(d => d.Id == id, trackChanges)
			.FirstOrDefaultAsync();

		return doctor;
	}

	public void UpdateDoctor(Doctor doctor)
	{
		Update(doctor);
	}
}
