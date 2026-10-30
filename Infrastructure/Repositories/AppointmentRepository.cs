using System.Globalization;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Value_Objects;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.RequestParameters;

namespace Infrastructure.Repositories;

public class AppointmentRepository : RepositoryBase<Appointment>, IAppointmentRepository
{
	public AppointmentRepository(AppDbContext dbContext) : base(dbContext) { }

	public void CreateAppointment(Appointment appointment)
	{
		Create(appointment);
	}

	public Task<IAppointmentBookingLock> AcquireBookingLockAsync(string doctorId)
	{
		if (!_dbContext.Database.IsSqlServer())
		{
			return Task.FromResult<IAppointmentBookingLock>(NoOpAppointmentBookingLock.Instance);
		}

		return SqlServerAppointmentBookingLock.AcquireAsync(_dbContext, doctorId);
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
						Reason = x.Reason,
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
			query = query.Where(x => string.Concat(x.User.FirstName, " ", x.User.LastName).Contains(requestParameters.ClientName));
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
			.ThenBy(x => x.Status)
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
				Reason = x.Reason,
				CancellationReason = x.CancellationReason,
				RejectionReason = x.RejectionReason,
			})
			.Paginate(requestParameters.PageNumber, requestParameters.PageSize)
			.ToListAsync();
	}

	/// <summary>
	/// Appointments are persisted as a start plus an elapsed duration, so the
	/// end is not a column SQL Server can compare directly. EF Core cannot
	/// translate DateTime.Add(TimeSpan) either. Read the small window that
	/// could possibly overlap, then apply the half-open check in memory.
	/// Durations are capped at eight hours by the request validator, so a
	/// two-day lookback covers every real appointment without scanning the
	/// doctor's whole history.
	/// </summary>
	private const int AppointmentLookbackDays = 2;

	private static DateTime LookbackStart(DateTime rangeStart)
	{
		return rangeStart <= DateTime.MinValue.AddDays(AppointmentLookbackDays)
			? DateTime.MinValue
			: rangeStart.AddDays(-AppointmentLookbackDays);
	}

	/// <summary>
	/// Active appointments (Pending + Confirmed) whose [StartTime, EndTime)
	/// window intersects the given calendar day. Pending requests reserve
	/// their slot too, otherwise two patients could both be told a slot is
	/// free while the doctor still has to choose only one of them.
	/// </summary>
	public async Task<IEnumerable<Appointment>> GetActiveAppointmentsByDoctorIdAndDate(string doctorId, DateTime date, bool trackChanges)
	{
		var dayStart = date.Date;
		var dayEnd = dayStart.AddDays(1);
		var lookbackStart = LookbackStart(dayStart);

		var candidates = await FindByCondition(x =>
				x.DoctorId == doctorId &&
				x.StartTime < dayEnd &&
				x.StartTime >= lookbackStart &&
				(x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Confirmed),
				trackChanges)
			.Select(x => new Appointment
			{
				Id = x.Id,
				StartTime = x.StartTime,
				Duration = x.Duration,
				Status = x.Status,
			})
			.ToListAsync();

		return candidates
			.Where(x => x.StartTime.Add(x.Duration) > dayStart)
			.ToList();
	}

	/// <inheritdoc />
	public async Task<IEnumerable<Appointment>> GetActiveAppointmentsByDoctorIdInRange(string doctorId, DateTime rangeStart, DateTime rangeEnd, bool trackChanges)
	{
		var lookbackStart = LookbackStart(rangeStart);

		var candidates = await FindByCondition(x =>
				x.DoctorId == doctorId &&
				x.StartTime < rangeEnd &&
				x.StartTime >= lookbackStart &&
				(x.Status == AppointmentStatus.Pending || x.Status == AppointmentStatus.Confirmed),
				trackChanges)
			.Select(x => new Appointment
			{
				Id = x.Id,
				StartTime = x.StartTime,
				Duration = x.Duration,
				Status = x.Status,
			})
			.ToListAsync();

		return candidates
			.Where(x => x.StartTime.Add(x.Duration) > rangeStart)
			.ToList();
	}

	public async Task<Appointment?> GetById(int appointmentId, bool trackChanges)
	{
		if (!trackChanges)
		{
			return await FindByCondition(x => x.Id == appointmentId, false)
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
					Reason = x.Reason,
					CancellationReason = x.CancellationReason,
					RejectionReason = x.RejectionReason,
				})
				.FirstOrDefaultAsync();
		}

		var appointment = await FindByCondition(x => x.Id == appointmentId, true)
			.Include(x => x.User)
			.Include(x => x.Doctor)
			.FirstOrDefaultAsync();

		if (appointment is not null)
		{
			// NotMapped fields are populated for the response mapping while the
			// persisted entity stays the tracked instance.
			appointment.ClientName = appointment.User.FullName;
			appointment.DoctorName = appointment.Doctor.FullName;
			appointment.DoctorPhotoUrl = appointment.Doctor.PhotoUrl;
			appointment.ClientPhotoUrl = appointment.User.PhotoUrl;
			appointment.ClientEmail = appointment.User.Email!;
			appointment.DoctorEmail = appointment.Doctor.Email!;
		}

		return appointment;
	}

	public async Task<IEnumerable<Appointment>> GetByUserId(string userId, MyAppointmentsRequestParameters requestParameters, bool trackChanges)
	{
		var query = FindByCondition(x => x.UserId == userId, trackChanges)
			.Include(x => x.User)
			.Include(x => x.Doctor)
			.AsQueryable();

		if (!string.IsNullOrEmpty(requestParameters.DoctorName))
		{
			query = query.Where(x => string.Concat(x.Doctor.FirstName, " ", x.Doctor.LastName).Contains(requestParameters.DoctorName));
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
				Reason = x.Reason,
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

	public async Task<(int totalAppointments, decimal totalProfit)> GetDoctorStats(string doctorId)
	{
		// A dashboard scalar must not materialize the doctor's appointment
		// history. Completed is also the only status that represents revenue:
		// pending, confirmed, cancelled and rejected rows are not profit.
		var stats = await FindByCondition(
				x => x.DoctorId == doctorId && x.Status == AppointmentStatus.Completed,
				false)
			.GroupBy(_ => 1)
			.Select(group => new
			{
				TotalAppointments = group.Count(),
				TotalProfit = group.Sum(appointment => appointment.Fees)
			})
			.FirstOrDefaultAsync();

		return stats is null ? (0, 0m) : (stats.TotalAppointments, stats.TotalProfit);
	}
	public async Task<int> GetDoctorCompletedAppointmentsCount(string doctorId)
	{
		return await FindByCondition(
				appointment => appointment.DoctorId == doctorId &&
					appointment.Status == AppointmentStatus.Completed,
				false)
			.CountAsync();
	}

	public async Task<(List<WeekdayAppointmentCount> weekdayCounts, List<MonthlyAppointmentCount> monthlyCounts)> GetDoctorAppointmentCounts(string doctorId)
	{
		var appointments = await FindByCondition(x => x.DoctorId == doctorId, false)
			.ToListAsync();

		var weekdayCounts = appointments
			.GroupBy(x => x.StartTime.DayOfWeek)
			.Select(x => new WeekdayAppointmentCount
			{
				DayOfWeek = x.Key,
				Count = x.Count()
			})
			.OrderBy(x => x.DayOfWeek)
			.ToList();

		var monthlyCounts = appointments
			.GroupBy(x => x.StartTime.Month)
			.Select(x => new MonthlyAppointmentCount
			{
				Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(x.Key),
				Count = x.Count()
			})
			.OrderBy(x => DateTime.ParseExact(x.Month, "MMM", CultureInfo.CurrentCulture).Month)
			.ToList();

		return (weekdayCounts, monthlyCounts);
	}

	public async Task<List<AppointmentStatusCount>> GetAppointmentStatusCounts(string doctorId)
	{
		var statusCounts = await FindByCondition(x => x.DoctorId == doctorId, false)
			.GroupBy(x => x.Status)
			.Select(x => new AppointmentStatusCount
			{
				Status = x.Key,
				Count = x.Count()
			})
			.ToListAsync();

		return statusCounts;
	}

	public async Task<int> GetTotalAppointments()
	{
		return await FindAll(false).CountAsync();
	}

	public Task<int> CountDoctorAppointmentsAsync(
		string doctorId,
		AppointmentStatus status,
		DateTime? startFrom,
		DateTime? startBefore)
	{
		var query = FindByCondition(
			appointment => appointment.DoctorId == doctorId && appointment.Status == status,
			false);

		if (startFrom.HasValue)
		{
			query = query.Where(appointment => appointment.StartTime >= startFrom.Value);
		}

		if (startBefore.HasValue)
		{
			query = query.Where(appointment => appointment.StartTime < startBefore.Value);
		}

		return query.CountAsync();
	}

	public Task<int> CountDistinctPatientsSinceAsync(string doctorId, DateTime since)
	{
		// Cancelled and rejected requests never represented care, so they do not
		// make a patient "active" on the dashboard.
		return FindByCondition(
			appointment => appointment.DoctorId == doctorId &&
				appointment.StartTime >= since &&
				appointment.Status != AppointmentStatus.Cancelled &&
				appointment.Status != AppointmentStatus.Rejected,
			false)
			.Select(appointment => appointment.UserId)
			.Distinct()
			.CountAsync();
	}

	public async Task<IReadOnlyList<TimeSpan>> GetCompletedDurationsSinceAsync(string doctorId, DateTime since)
	{
		return await FindByCondition(
			appointment => appointment.DoctorId == doctorId &&
				appointment.Status == AppointmentStatus.Completed &&
				appointment.StartTime >= since,
			false)
			.Select(appointment => appointment.Duration)
			.ToListAsync();
	}
}

