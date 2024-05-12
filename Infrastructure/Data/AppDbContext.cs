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
		builder.Entity<User>().ToTable("Users")
			.HasMany(d => d.Appointments)
			.WithOne(d => d.User)
			.OnDelete(DeleteBehavior.NoAction); ;

		builder.Entity<Doctor>().ToTable("Doctors")
			.HasMany(d => d.Appointments)
			.WithOne(d => d.Doctor)
			.OnDelete(DeleteBehavior.NoAction);

		builder.Entity<WeeklySchedule>()
			.HasMany(d => d.AvailableDays)
			.WithOne()
			.HasForeignKey(d => d.WeeklyScheduleId);

		builder.Entity<BaseUser>().ToTable("BaseUsers");



		base.OnModelCreating(builder);
	}

	public DbSet<Post> Posts { get; set; }
	public DbSet<Comment> Comments { get; set; }
	public DbSet<Reply> Replies { get; set; }
	public DbSet<Notification> Notifications { get; set; }
	public DbSet<User> AppUsers { get; set; }
	public DbSet<Doctor> Doctors { get; set; }
	public DbSet<User> Clients { get; set; }
	//public DbSet<Appointment> Appointments { get; set; }
	//public DbSet<TimeSlot> TimeSlots { get; set; }
	//public DbSet<WeeklySchedule> WeeklySchedules { get; set; }


}
