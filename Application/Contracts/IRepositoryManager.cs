using Domain.Repositories;

namespace Application.Contracts;
public interface IRepositoryManager
{
	IPostRepository Posts { get; }
	IPostMediaAssetRepository PostMediaAssets { get; }
	ICommentRepository Comments { get; }
	IReplyRepository Replies { get; }
	INotificationRepository Notifications { get; }
	IChatRoomRepository ChatRooms { get; }
	IChatMessageRepository ChatMessages { get; }
	IChatMessageReportRepository ChatMessageReports { get; }
	IDoctorRepository Doctors { get; }
	IUserRepository Users { get; }
	IProfileRepository Profiles { get; }
	IAppointmentRepository Appointments { get; }
	IDoctorSchedulingRepository DoctorScheduling { get; }
	IDoctorWorkspaceRepository DoctorWorkspace { get; }
	IDepressionTestResultRepository DepressionTestResults { get; }

	Task SaveAsync();
}
