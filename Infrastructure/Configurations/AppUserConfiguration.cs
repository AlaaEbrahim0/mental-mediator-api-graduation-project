using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<BaseUser>
{

	public void Configure(EntityTypeBuilder<BaseUser> builder)
	{
		builder.ToTable("BaseUsers");

		builder
			.HasMany(x => x.Notifications)
			.WithOne(x => x.AppUser)
			.HasPrincipalKey(x => x.Id)
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.HasMany(x => x.Posts)
			.WithOne(x => x.AppUser)
			.HasPrincipalKey(x => x.Id)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasQueryFilter(x => !x.isDeleted);
	}
}
