using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<BaseUser>
{

	public void Configure(EntityTypeBuilder<BaseUser> builder)
	{
		builder.ToTable("BaseUsers");

		builder.Property(user => user.Headline).HasMaxLength(160);
		builder.Property(user => user.Pronouns).HasMaxLength(40);
		builder.Property(user => user.CoverPhotoUrl).HasMaxLength(1000);
		builder.Property(user => user.WebsiteUrl).HasMaxLength(500);
		builder.Property(user => user.LinkedInUrl).HasMaxLength(500);
		builder.Property(user => user.TwitterUrl).HasMaxLength(500);
		builder.Property(user => user.InstagramUrl).HasMaxLength(500);
		builder.Property(user => user.FacebookUrl).HasMaxLength(500);
		builder.Property(user => user.YouTubeUrl).HasMaxLength(500);
		builder.Property(user => user.Languages).HasMaxLength(1000);
		builder.Property(user => user.Interests).HasMaxLength(2000);
		builder.Property(user => user.ContactEmail).HasMaxLength(200);
		builder.Property(user => user.ContactPhone).HasMaxLength(40);
		builder.Property(user => user.ContactAddress).HasMaxLength(300);
		builder.Property(user => user.IsProfilePublic).HasDefaultValue(false);

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

		builder.HasQueryFilter(x => !x.IsDeleted);
	}
}
