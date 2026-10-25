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
	Task<IEnumerable<Appointment>> GetByDoctorIdAndDate(string doctorId, DateTime date, bool trackChanges);

	/// <summary>
	/// Appointments that currently reserve the doctor's time for the given
	/// date: Pending requests hold their slot until the doctor confirms or
	/// rejects them, and Confirmed appointments are booked. Cancelled,
	/// rejected and completed appointments do not reserve time.
	/// </summary>
	Task<IEnumerable<Appointment>> GetActiveAppointmentsByDoctorIdAndDate(string doctorId, DateTime date, bool trackChanges);

	/// <summary>
	/// Appointments that reserve the doctor's time inside [rangeStart,
	/// rangeEnd) regardless of which day they start on. Used by schedule
	/// change conflict checks that can span several days.
	/// </summary>
	Task<IEnumerable<Appointment>> GetActiveAppointmentsByDoctorIdInRange(string doctorId, DateTime rangeStart, DateTime rangeEnd, bool trackChanges);
	Task<(int totalAppointments, decimal totalProfit)> GetDoctorStats(string doctorId);
	Task<(List<WeekdayAppointmentCount> weekdayCounts, List<MonthlyAppointmentCount> monthlyCounts)> GetDoctorAppointmentCounts(string doctorId);
	Task<List<AppointmentStatusCount>> GetAppointmentStatusCounts(string doctorId);
	Task<int> GetTotalAppointments();
}