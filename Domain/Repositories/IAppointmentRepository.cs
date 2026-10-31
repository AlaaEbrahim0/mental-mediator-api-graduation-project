using Domain.Entities;
using Domain.Enums;
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
	/// Chronological history for one patient, optionally limited to one doctor.
	/// Used by the patient-history views; the caller enforces who may ask.
	/// </summary>
	Task<IReadOnlyList<Appointment>> GetHistoryForPatientAsync(
		string patientId,
		string? doctorId,
		int take,
		bool trackChanges);

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

	/// <summary>
	/// Counts one doctor's appointments by status inside an optional start-time
	/// window. A dashboard reads four of these; each is a single aggregate so the
	/// numbers do not depend on loading the doctor's whole history.
	/// </summary>
	Task<int> CountDoctorAppointmentsAsync(
		string doctorId,
		AppointmentStatus status,
		DateTime? startFrom,
		DateTime? startBefore);

	Task<int> CountDistinctPatientsSinceAsync(string doctorId, DateTime since);

	/// <summary>
	/// Scheduled durations of completed consultations since <paramref name="since"/>.
	/// Only the column is projected because SQL cannot average a TimeSpan; the
	/// caller computes the mean.
	/// </summary>
	Task<IReadOnlyList<TimeSpan>> GetCompletedDurationsSinceAsync(string doctorId, DateTime since);
	Task<(List<WeekdayAppointmentCount> weekdayCounts, List<MonthlyAppointmentCount> monthlyCounts)> GetDoctorAppointmentCounts(string doctorId);
	Task<List<AppointmentStatusCount>> GetAppointmentStatusCounts(string doctorId);
	Task<int> GetTotalAppointments();
}
