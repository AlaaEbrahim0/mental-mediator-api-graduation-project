using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Infrastructure.Repositories;

public class AppointmentRepository : RepositoryBase<Appointment>, IAppointementRepository
{
	public AppointmentRepository(AppDbContext dbContext) : base(dbContext) { }

	public void CreateAppointment(Appointment appointment)
	{
		Create(appointment);
	}

	public void DeleteAppointment(Appointment appointment)
	{
		Delete(appointment);
	}

	public async Task<IEnumerable<Appointment>> GetAll(RequestParameters requestParameters, bool trackChanges)
	{
		return await
			FindAll(trackChanges)
			.Include(x => x.User)
			.Include(x => x.Doctor)
			.OrderByDescending(x => x.StartTime)
			.Select(x => new Appointment
			{
				Id = x.Id,
				DoctorId = x.DoctorId,
				UserId = x.UserId,
				ClientName = x.User.FullName,
				DoctorName = x.Doctor.FullName,
				DoctorPhotoUrl = x.Doctor.PhotoUrl,
				ClientPhotoUrl = x.User.PhotoUrl,
				ClientEmail = x.User.Email!,
				DoctorEmail = x.Doctor.Email!,
				StartTime = x.StartTime,
				Duration = x.Duration,
				Status = x.Status,
				Location = x.Location,
				CancellationReason = x.CancellationReason,
				RejectionReason = x.RejectionReason,
			})
			.Paginate(requestParameters.PageNumber, requestParameters.PageSize)
			.ToListAsync();
	}

	public async Task<IEnumerable<Appointment>> GetByDoctorId(string doctorId, RequestParameters requestParameters, bool trackChanges)
	{
		return await
			FindByCondition(x =>
				x.DoctorId == doctorId,
				trackChanges)
			.Include(x => x.User)
			.Include(x => x.Doctor)
			.OrderByDescending(x => x.StartTime)
			.Select(x => new Appointment
			{
				Id = x.Id,
				DoctorId = x.DoctorId,
				UserId = x.UserId,
				ClientName = x.User.FullName,
				DoctorName = x.Doctor.FullName,
				DoctorPhotoUrl = x.Doctor.PhotoUrl,
				ClientPhotoUrl = x.User.PhotoUrl,
				ClientEmail = x.User.Email!,
				DoctorEmail = x.Doctor.Email!,
				StartTime = x.StartTime,
				Duration = x.Duration,
				Status = x.Status,
				Location = x.Location,
				CancellationReason = x.CancellationReason,
				RejectionReason = x.RejectionReason,
			})

			.Paginate(requestParameters.PageNumber, requestParameters.PageSize)
			.ToListAsync();
	}

	public async Task<Appointment?> GetById(int appointementId, bool trackChanges)
	{
		return await
			FindByCondition(x =>
				x.Id == appointementId,
				trackChanges)
			.Include(x => x.User)
			.Include(x => x.Doctor)
			.Select(x => new Appointment
			{
				Id = x.Id,
				DoctorId = x.DoctorId,
				UserId = x.UserId,
				ClientName = x.User.FullName,
				DoctorName = x.Doctor.FullName,
				DoctorPhotoUrl = x.Doctor.PhotoUrl,
				ClientPhotoUrl = x.User.PhotoUrl,
				ClientEmail = x.User.Email!,
				DoctorEmail = x.Doctor.Email!,
				StartTime = x.StartTime,
				Duration = x.Duration,
				Status = x.Status,
				Location = x.Location,
				CancellationReason = x.CancellationReason,
				RejectionReason = x.RejectionReason,
			})
			.FirstOrDefaultAsync();


	}

	public async Task<IEnumerable<Appointment>> GetByUserId(string userId, RequestParameters requestParameters, bool trackChanges)
	{
		return await
			FindByCondition(x =>
				x.UserId == userId,
				trackChanges)
			.Include(x => x.User)
			.Include(x => x.Doctor)
			.OrderByDescending(x => x.StartTime)
			.Select(x => new Appointment
			{
				Id = x.Id,
				DoctorId = x.DoctorId,
				UserId = x.UserId,
				ClientName = x.User.FullName,
				DoctorName = x.Doctor.FullName,
				DoctorPhotoUrl = x.Doctor.PhotoUrl,
				ClientPhotoUrl = x.User.PhotoUrl,
				ClientEmail = x.User.Email!,
				DoctorEmail = x.Doctor.Email!,
				StartTime = x.StartTime,
				Duration = x.Duration,
				Status = x.Status,
				Location = x.Location,
				CancellationReason = x.CancellationReason,
				RejectionReason = x.RejectionReason,
			})
			.Paginate(requestParameters.PageNumber, requestParameters.PageSize)
			.ToListAsync();
	}

	public void UpdateAppointment(Appointment appointment)
	{
		Update(appointment);
	}
}

