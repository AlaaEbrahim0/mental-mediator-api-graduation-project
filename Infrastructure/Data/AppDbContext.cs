using Infrastructure.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;
public class AppDbContext: IdentityDbContext<AppUser> 
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new RoleConfiguration());

        base.OnModelCreating(builder);
    }
}
