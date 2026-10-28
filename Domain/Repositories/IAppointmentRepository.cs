using Domain.Entities;
using Domain.Value_Objects;
using Shared.RequestParameters;

namespace Domain.Repositories;

public interface IAppointmentRepository
{
	Task<IEnumerable<Appointment>> GetAll(AppointmentRequestParameters request, bool trackChanges);
	Task<IEnumerable<Appointment>> GetByUserId(string userId, MyAppointmentsRequestParameters request, bool trackChanges);
	Task<IEnumerable<Appointment>> GetByDoctorId(string userId, MyAppointmentsRequestParameters request, bool trackChanges);
	void CreateAppointment(Appointment appointment);
	void DeleteAppointment(Appointment appointment);
	void UpdateAppointment(Appointment appointment);
	Task<Appointment?> GetById(int appointmentId, bool trackChanges);

	/// <summary>
	/// Appointments that currently reserve the doctor's time: Pending requests
	/// hold their slot until confirmed or rejected, and Confirmed appointments
	/// are booked. Cancelled, rejected and completed appointments do not
	/// reserve time.
	/// </summary>
	Task<IEnumerable<Appointment>> GetActiveAppointmentsByDoctorIdAndDate(string doctorId, DateTime date, bool trackChanges);

	/// <summary>
	/// Active appointments whose [StartTime, StartTime + Duration) window
	/// intersects [rangeStart, rangeEnd).
	/// </summary>
	Task<IEnumerable<Appointment>> GetActiveAppointmentsByDoctorIdInRange(string doctorId, DateTime rangeStart, DateTime rangeEnd, bool trackChanges);

	/// <summary>
	/// Acquires a database-backed, transaction-scoped lock for the doctor.
	/// Must be held while availability is revalidated and the appointment is
	/// inserted, then committed before the lock is disposed.
	/// </summary>
	Task<IAppointmentBookingLock> AcquireBookingLockAsync(string doctorId);

	Task<(int totalAppointments, decimal totalProfit)> GetDoctorStats(string doctorId);
	Task<int> GetDoctorCompletedAppointmentsCount(string doctorId);
	Task<(List<WeekdayAppointmentCount> weekdayCounts, List<MonthlyAppointmentCount> monthlyCounts)> GetDoctorAppointmentCounts(string doctorId);
	Task<List<AppointmentStatusCount>> GetAppointmentStatusCounts(string doctorId);
	Task<int> GetTotalAppointments();
}
