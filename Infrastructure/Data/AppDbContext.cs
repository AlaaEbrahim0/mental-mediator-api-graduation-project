using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;
public class AppDbContext : IdentityDbContext<AppUser>
{
	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base(options)
	{

	}
	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);
	}
	public DbSet<Post> Posts { get; set; }
	public DbSet<Comment> Comments { get; set; }
	public DbSet<Reply> Replies { get; set; }
	public DbSet<Notification> Notifications { get; set; }
}
