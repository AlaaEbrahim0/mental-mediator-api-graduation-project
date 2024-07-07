using Application.Dtos.AppointmentDtos;
using Shared;
using Shared.RequestParameters;

namespace Application.Contracts;
public interface IAppointmentService
{
	Task<Result<List<AppointmentResponse>>> GetAppointments(AppointmentRequestParameters request);
	Task<Result<List<AppointmentResponse>>> GetDoctorAppointments(MyAppointmentsRequestParameters request);
	Task<Result<List<AppointmentResponse>>> GetClientAppointments(MyAppointmentsRequestParameters request);
	Task<Result<AppointmentResponse?>> GetAppointment(int id);
	Task<Result<AppointmentResponse>> CreateAppointment(string doctorId, CreateAppointmentRequest request);
	Task<Result<AppointmentResponse?>> ConfirmAppointment(int id);
	Task<Result<AppointmentResponse?>> RejectAppointment(int id, RejectAppointmentRequest request);
	Task<Result<AppointmentResponse?>> CancelAppointment(int id, string? cancellationReason);
}
