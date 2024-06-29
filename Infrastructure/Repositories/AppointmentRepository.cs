using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Infrastructure.Repositories;

public class AppointmentRepository : RepositoryBase<Appointment>, IAppointmentRepository
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

	public async Task<IEnumerable<Appointment>> GetAll(AppointmentRequestParameters requestParameters, bool trackChanges)
	{
		var query = FindAll(trackChanges)
					.Include(x => x.User)
					.Include(x => x.Doctor)
						.AsQueryable();

		if (!string.IsNullOrEmpty(requestParameters.DoctorId))
		{
			query = query.Where(x => x.DoctorId == requestParameters.DoctorId);
		}

		if (!string.IsNullOrEmpty(requestParameters.UserId))
		{
			query = query.Where(x => x.UserId == requestParameters.UserId);
		}

		if (requestParameters.StartDate.HasValue)
		{
			query = query.Where(x => x.StartTime >= requestParameters.StartDate.Value);
		}

		if (requestParameters.EndDate.HasValue)
		{
			query = query.Where(x => x.StartTime <= requestParameters.EndDate.Value);
		}

		if (!string.IsNullOrEmpty(requestParameters.Status) &&
			Enum.TryParse(requestParameters.Status, out AppointmentStatus status))
		{
			query = query.Where(x => x.Status == status);
		}

		return await query
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
						Fees = x.Fees,
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

	public async Task<IEnumerable<Appointment>> GetByDoctorId(string doctorId, MyAppointmentsRequestParameters requestParameters, bool trackChanges)
	{
		var query = FindByCondition(x => x.DoctorId == doctorId, trackChanges)
			.Include(x => x.User)
			.Include(x => x.Doctor)
			.AsQueryable();

		if (!string.IsNullOrEmpty(requestParameters.ClientName))
		{
			query = query.Where(x => x.ClientName.Contains(requestParameters.ClientName));
		}

		if (requestParameters.StartDate.HasValue)
		{
			query = query.Where(x => x.StartTime >= requestParameters.StartDate.Value);
		}

		if (requestParameters.EndDate.HasValue)
		{
			query = query.Where(x => x.StartTime <= requestParameters.EndDate.Value);
		}

		if (!string.IsNullOrEmpty(requestParameters.Status) &&
			Enum.TryParse(requestParameters.Status, out AppointmentStatus status))
		{
			query = query.Where(x => x.Status == status);
		}


		return await query
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
				Fees = x.Fees,
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

	public async Task<IEnumerable<Appointment>> GetByDoctorIdAndDate(string doctorId, DateTime date, bool trackChanges)
	{
		return await
			FindByCondition(x =>
				x.DoctorId == doctorId &&
				x.StartTime >= date && x.StartTime < date.AddDays(1) &&
				x.Status != AppointmentStatus.Confirmed
				,
				trackChanges)
			.Select(x => new Appointment
			{
				StartTime = x.StartTime,
				Duration = x.Duration,
				Status = x.Status,
			})
			.ToListAsync();
	}

	public async Task<Appointment?> GetById(int appointmentId, bool trackChanges)
	{
		return await
			FindByCondition(x =>
				x.Id == appointmentId,
				trackChanges)
			.Include(x => x.User)
			.Include(x => x.Doctor)
			.Select(x => new Appointment
			{
				Id = x.Id,
				DoctorId = x.DoctorId,
				UserId = x.UserId,
				Fees = x.Fees,
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

	public async Task<IEnumerable<Appointment>> GetByUserId(string userId, MyAppointmentsRequestParameters requestParameters, bool trackChanges)
	{
		var query = FindByCondition(x => x.DoctorId == userId, trackChanges)
			.Include(x => x.User)
			.Include(x => x.Doctor)
			.AsQueryable();

		if (!string.IsNullOrEmpty(requestParameters.DoctorName))
		{
			query = query.Where(x => x.DoctorName.Contains(requestParameters.DoctorName));
		}

		if (requestParameters.StartDate.HasValue)
		{
			query = query.Where(x => x.StartTime >= requestParameters.StartDate.Value);
		}

		if (requestParameters.EndDate.HasValue)
		{
			query = query.Where(x => x.StartTime <= requestParameters.EndDate.Value);
		}

		if (!string.IsNullOrEmpty(requestParameters.Status) &&
			Enum.TryParse(requestParameters.Status, out AppointmentStatus status))
		{
			query = query.Where(x => x.Status == status);
		}


		return await query
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
				Fees = x.Fees,
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

