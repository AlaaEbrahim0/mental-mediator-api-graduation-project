using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DoctorScheduleBlockRepository : RepositoryBase<DoctorScheduleBlock>, IDoctorScheduleBlockRepository
{
	public DoctorScheduleBlockRepository(AppDbContext dbContext) : base(dbContext) { }

	public async Task<IEnumerable<DoctorScheduleBlock>> GetByDoctorId(string doctorId, bool trackChanges)
	{
		return await FindByCondition(x => x.DoctorId == doctorId, trackChanges)
			.OrderBy(x => x.StartTime)
			.ToListAsync();
	}

	public async Task<DoctorScheduleBlock?> GetById(int id, bool trackChanges)
	{
		return await FindByCondition(x => x.Id == id, trackChanges)
			.FirstOrDefaultAsync();
	}

	public void CreateScheduleBlock(DoctorScheduleBlock block)
	{
		Create(block);
	}

	public void DeleteScheduleBlock(DoctorScheduleBlock block)
	{
		Delete(block);
	}
}