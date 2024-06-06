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
	public DbSet<Comment> Comments { get; set; }
	public DbSet<Reply> Replies { get; set; }
	public DbSet<Notification> Notifications { get; set; }
	public DbSet<User> AppUsers { get; set; }
	public DbSet<Doctor> Doctors { get; set; }
	public DbSet<User> Clients { get; set; }
	public DbSet<Admin> Admins { get; set; }
	public DbSet<Appointment> Appointments { get; set; }
	public DbSet<DoctorScheduleWeekDay> ScheduleWeekDays { get; set; }

}
