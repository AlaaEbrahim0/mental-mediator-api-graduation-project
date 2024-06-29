using Domain.Entities;
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
}