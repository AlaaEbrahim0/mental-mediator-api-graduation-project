using Application.Contracts;
using Domain.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositoryManager : IRepositoryManager
{
	private readonly AppDbContext _dbContext;
	private readonly Lazy<IPostRepository> postRepository;
	private readonly Lazy<IPostMediaAssetRepository> postMediaAssetRepository;
	private readonly Lazy<ICommentRepository> commentRepository;
	private readonly Lazy<IReplyRepository> replyRepository;
	private readonly Lazy<INotificationRepository> notificationRepository;
	private readonly Lazy<IDoctorRepository> doctorRepository;
	private readonly Lazy<IUserRepository> userRepository;
	private readonly Lazy<IAppointmentRepository> appointementRepository;
	private readonly Lazy<IDepressionTestResultRepository> depressionTestResultRepository;
	private readonly Lazy<IChatRoomRepository> chatRoomRepository;
	private readonly Lazy<IChatMessageRepository> chatMessageRepository;
	private readonly Lazy<IChatMessageReportRepository> chatMessageReportRepository;
	private readonly Lazy<IDoctorSchedulingRepository> doctorSchedulingRepository;
	private readonly Lazy<IProfileRepository> profileRepository;
	private readonly Lazy<IDoctorWorkspaceRepository> doctorWorkspaceRepository;

	public RepositoryManager(AppDbContext dbContext)
	{
		_dbContext = dbContext;
		postRepository = new(() => new PostRepository(dbContext));
		postMediaAssetRepository = new(() => new PostMediaAssetRepository(dbContext));
		commentRepository = new(() => new CommentRepository(dbContext));
		replyRepository = new(() => new ReplyRepository(dbContext));
		doctorRepository = new(() => new DoctorRepository(dbContext));
		userRepository = new(() => new UserRepository(dbContext));
		notificationRepository = new(() => new NotificationRepository(dbContext));
		appointementRepository = new(() => new AppointmentRepository(dbContext));
		depressionTestResultRepository = new(() => new DepressionTestResultRepository(dbContext));
		chatRoomRepository = new(() => new ChatRoomRepository(dbContext));
		chatMessageRepository = new(() => new ChatMessageRepository(dbContext));
		chatMessageReportRepository = new(() => new ChatMessageReportRepository(dbContext));
		doctorSchedulingRepository = new(() => new DoctorSchedulingRepository(dbContext));
		profileRepository = new(() => new ProfileRepository(dbContext));
		doctorWorkspaceRepository = new(() => new DoctorWorkspaceRepository(dbContext));
	}

	public IPostRepository Posts => postRepository.Value;
	public IPostMediaAssetRepository PostMediaAssets => postMediaAssetRepository.Value;
	public ICommentRepository Comments => commentRepository.Value;
	public IReplyRepository Replies => replyRepository.Value;
	public INotificationRepository Notifications => notificationRepository.Value;
	public IChatRoomRepository ChatRooms => chatRoomRepository.Value;
	public IChatMessageRepository ChatMessages => chatMessageRepository.Value;
	public IChatMessageReportRepository ChatMessageReports => chatMessageReportRepository.Value;
	public IDoctorRepository Doctors => doctorRepository.Value;
	public IUserRepository Users => userRepository.Value;
	public IAppointmentRepository Appointments => appointementRepository.Value;
	public IDoctorSchedulingRepository DoctorScheduling => doctorSchedulingRepository.Value;
	public IProfileRepository Profiles => profileRepository.Value;
	public IDoctorWorkspaceRepository DoctorWorkspace => doctorWorkspaceRepository.Value;
	public IDepressionTestResultRepository DepressionTestResults => depressionTestResultRepository.Value;

	public async Task SaveAsync() => await _dbContext.SaveChangesAsync();
}
