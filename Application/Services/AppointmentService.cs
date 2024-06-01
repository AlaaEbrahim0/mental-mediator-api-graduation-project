using Domain.Entities;
using Shared;

namespace Application.Services;
public interface IAppointmentService
{
	Task<Result<List<Appointment>>> GetAppointements(RequestParameters request);

}
