using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DoctorRepository : RepositoryBase<Doctor>, IDoctorRepository
{
	public DoctorRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public async Task<Doctor?> GetById(string id, bool trackChanges)
	{
		var doctor = await FindByCondition(d => d.Id == id, trackChanges)
			.Include(d => d.WeeklySchedule)
			.FirstOrDefaultAsync();
		return doctor;
	}

	public void UpdateDoctor(Doctor doctor)
	{
		Update(doctor);
	}
}
