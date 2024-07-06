using Domain.Repositories;

namespace Application.Contracts;
public interface IRepositoryManager
{
	IPostRepository Posts { get; }
	ICommentRepository Comments { get; }
	IReplyRepository Replies { get; }
	INotificationRepository Notifications { get; }
	IDoctorRepository Doctors { get; }
	IUserRepository Users { get; }
	IDoctorScheduleRepository DoctorSchedule { get; }
	IAppointmentRepository Appointments { get; }
	IDepressionTestResultRepository DepressionTestResults { get; }

	Task SaveAsync();
}
