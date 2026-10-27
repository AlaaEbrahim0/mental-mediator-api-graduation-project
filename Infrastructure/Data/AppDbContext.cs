using System.Reflection;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;
public class AppDbContext : IdentityDbContext<BaseUser>
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{

	}
	protected override void OnModelCreating(ModelBuilder builder)
	{
		builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

		base.OnModelCreating(builder);


	}

	public DbSet<Post> Posts { get; set; }
	public DbSet<PostLike> PostLikes { get; set; }
	public DbSet<Comment> Comments { get; set; }
	public DbSet<Reply> Replies { get; set; }
	public DbSet<Notification> Notifications { get; set; }
	public DbSet<ChatRoom> ChatRooms { get; set; }
	public DbSet<ChatRoomMember> ChatRoomMembers { get; set; }
	public DbSet<ChatMessage> ChatMessages { get; set; }
	public DbSet<ChatMessageReport> ChatMessageReports { get; set; }
	public DbSet<User> AppUsers { get; set; }
	public DbSet<Doctor> Doctors { get; set; }
	public DbSet<User> Clients { get; set; }
	public DbSet<Admin> Admins { get; set; }
	public DbSet<Appointment> Appointments { get; set; }
	public DbSet<DoctorAvailabilityPeriod> DoctorAvailabilityPeriods { get; set; }
	public DbSet<DoctorTimeOff> DoctorTimeOff { get; set; }
	public DbSet<DoctorSchedulingSettings> DoctorSchedulingSettings { get; set; }

}
