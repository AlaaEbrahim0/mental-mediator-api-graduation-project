using Domain.Entities;
using Domain.Value_Objects;
using Shared;

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
	Task<(int totalAppointments, decimal totalProfit)> GetDoctorStats(string doctorId);
	Task<(List<WeekdayAppointmentCount> weekdayCounts, List<MonthlyAppointmentCount> monthlyCounts)> GetDoctorAppointmentCounts(string doctorId);
	Task<List<AppointmentStatusCount>> GetAppointmentStatusCounts(string doctorId);
}

