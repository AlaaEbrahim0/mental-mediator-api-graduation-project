using Application.Dtos.AppointmentDtos;
using Shared;

namespace Application.Services;
public interface IAppointmentService
{
	Task<Result<List<AppointmentResponse>>> GetAppointements(RequestParameters request);
	Task<Result<List<AppointmentResponse>>> GetDoctorAppointments(RequestParameters request);
	Task<Result<List<AppointmentResponse>>> GetClientAppointments(RequestParameters request);
	Task<Result<AppointmentResponse?>> GetAppointment(int id);
	Task<Result<AppointmentResponse>> CreateAppointment(string doctorId, CreateAppointmentRequest request);
	Task<Result<AppointmentResponse?>> ConfirmAppointment(int id);
	Task<Result<AppointmentResponse?>> RejectAppointment(int id, string? rejectionReason);
	Task<Result<AppointmentResponse?>> CancelAppointment(int id, string? cancellationReason);
}
