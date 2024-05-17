using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{

	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder
			.ToTable("Users");

		builder
			.HasMany(d => d.Appointments)
			.WithOne(d => d.User)
			.OnDelete(DeleteBehavior.NoAction);

	}

}

