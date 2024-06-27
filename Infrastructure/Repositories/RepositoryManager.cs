using Application.Contracts;
using Domain.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositoryManager : IRepositoryManager
{
	private readonly AppDbContext _dbContext;
	private readonly Lazy<IPostRepository> postRepository;
	private readonly Lazy<ICommentRepository> commentRepository;
	private readonly Lazy<IReplyRepository> replyRepository;
	private readonly Lazy<INotificationRepository> notificationRepository;
	private readonly Lazy<IDoctorRepository> doctorRepository;
	private readonly Lazy<IUserRepository> userRepository;
	private readonly Lazy<IAppointmentRepository> appointementRepository;
	private readonly Lazy<IDoctorScheduleRepository> weeklyScheduleRepository;

	public RepositoryManager(AppDbContext dbContext)
	{
		_dbContext = dbContext;
		postRepository = new(() => new PostRepository(dbContext));
		commentRepository = new(() => new CommentRepository(dbContext));
		replyRepository = new(() => new ReplyRepository(dbContext));
		doctorRepository = new(() => new DoctorRepository(dbContext));
		userRepository = new(() => new UserRepository(dbContext));
		weeklyScheduleRepository = new(() => new DoctorScheduleRepository(dbContext));
		notificationRepository = new(() => new NotificationRepository(dbContext));
		appointementRepository = new(() => new AppointmentRepository(dbContext));
	}

	public IPostRepository Posts => postRepository.Value;
	public ICommentRepository Comments => commentRepository.Value;
	public IReplyRepository Replies => replyRepository.Value;
	public INotificationRepository Notifications => notificationRepository.Value;
	public IDoctorRepository Doctors => doctorRepository.Value;
	public IUserRepository Users => userRepository.Value;
	public IDoctorScheduleRepository DoctorSchedule => weeklyScheduleRepository.Value;
	public IAppointmentRepository Appointments => appointementRepository.Value;


	public async Task SaveAsync() => await _dbContext.SaveChangesAsync();
}
