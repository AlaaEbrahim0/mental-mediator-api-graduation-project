using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.RequestParameters;

namespace Infrastructure.Repositories;

public class DoctorRepository : RepositoryBase<Doctor>, IDoctorRepository
{
	public DoctorRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public async Task<IEnumerable<Doctor>> GetAll(DoctorRequestParameters requestParameters, bool trackChanges)
	{
		var doctorsQuery = FindAll(trackChanges);
		doctorsQuery = doctorsQuery.Where(d => !d.IsDeleted);

		if (!string.IsNullOrWhiteSpace(requestParameters.Name))
		{
			doctorsQuery = doctorsQuery.Where(d =>
				d.FirstName.Contains(requestParameters.Name) ||
				d.LastName.Contains(requestParameters.Name));
		}

		if (!string.IsNullOrWhiteSpace(requestParameters.Specialization) && Enum.TryParse<DoctorSpecialization>(requestParameters.Specialization,
			out DoctorSpecialization specialization))
		{
			doctorsQuery = doctorsQuery.Where(d => d.Specialization == specialization);

		}

		if (!string.IsNullOrWhiteSpace(requestParameters.Gender))
		{
			doctorsQuery = doctorsQuery.Where(d => d.Gender == requestParameters.Gender);
		}

		if (!string.IsNullOrWhiteSpace(requestParameters.City))
		{
			doctorsQuery = doctorsQuery.Where(d => d.City!.Contains(requestParameters.City));
		}

		if (requestParameters.MinFees.HasValue)
		{
			doctorsQuery = doctorsQuery.Where(d => d.SessionFees >= requestParameters.MinFees.Value);
		}

		if (requestParameters.MaxFees.HasValue)
		{
			doctorsQuery = doctorsQuery.Where(d => d.SessionFees <= requestParameters.MaxFees.Value);
		}
		if (requestParameters.IsAvailable.HasValue) doctorsQuery = doctorsQuery.Where(d => d.IsAvailable == requestParameters.IsAvailable.Value);

		// Skip/Take over an unordered query is not stable across pages, so pin
		// a deterministic order: available doctors first, then by name.
		doctorsQuery = doctorsQuery
			.OrderByDescending(d => d.IsAvailable)
			.ThenBy(d => d.FirstName)
			.ThenBy(d => d.LastName)
			.ThenBy(d => d.Id);

		var doctors = await doctorsQuery
			.Paginate(requestParameters.PageNumber, requestParameters.PageSize)
			.ToListAsync();

		return doctors;
	}

	public async Task<int> GetFilteredCount(DoctorRequestParameters requestParameters)
	{
		var query = FindAll(false).Where(d => !d.IsDeleted);

		if (!string.IsNullOrWhiteSpace(requestParameters.Name))
		{
			query = query.Where(d => d.FirstName.Contains(requestParameters.Name) || d.LastName.Contains(requestParameters.Name));
		}
		if (!string.IsNullOrWhiteSpace(requestParameters.Specialization) &&
			Enum.TryParse<DoctorSpecialization>(requestParameters.Specialization, out var specialization))
		{
			query = query.Where(d => d.Specialization == specialization);
		}
		if (!string.IsNullOrWhiteSpace(requestParameters.Gender))
		{
			query = query.Where(d => d.Gender == requestParameters.Gender);
		}
		if (!string.IsNullOrWhiteSpace(requestParameters.City))
		{
			query = query.Where(d => d.City!.Contains(requestParameters.City));
		}
		if (requestParameters.MinFees.HasValue)
		{
			query = query.Where(d => d.SessionFees >= requestParameters.MinFees.Value);
		}
		if (requestParameters.MaxFees.HasValue)
		{
			query = query.Where(d => d.SessionFees <= requestParameters.MaxFees.Value);
		}
		if (requestParameters.IsAvailable.HasValue)
		{
			query = query.Where(d => d.IsAvailable == requestParameters.IsAvailable.Value);
		}

		return await query.CountAsync();
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

	public void DeleteDoctor(Doctor doctor)
	{
		Delete(doctor);
	}

	public async Task<int> GetCount()
	{
		return await FindAll(false).CountAsync();
	}
}
