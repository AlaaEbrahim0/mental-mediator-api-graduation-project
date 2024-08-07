﻿using Domain.Entities;
using Shared.RequestParameters;

namespace Domain.Repositories;

public interface IDoctorRepository
{
	Task<IEnumerable<Doctor>> GetAll(DoctorRequestParameters requestParameters, bool trackChanges);
	Task<Doctor?> GetById(string id, bool trackChanges);
	void UpdateDoctor(Doctor doctor);
	void DeleteDoctor(Doctor doctor);
	Task<int> GetCount();
}
