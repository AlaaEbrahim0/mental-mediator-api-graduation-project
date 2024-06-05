using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;


public class PostConfiguration : IEntityTypeConfiguration<Post>
{

	public void Configure(EntityTypeBuilder<Post> builder)
	{
		builder
			.HasMany(p => p.Comments)
			.WithOne(c => c.Post)
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.Property(p => p.PostPhotoUrl)
			.HasMaxLength(1000);

		builder
			.HasIndex(p => p.PostedOn);

	}

}


public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{

	public void Configure(EntityTypeBuilder<Notification> builder)
	{

		builder
			.Property(p => p.NotifierPhotoUrl)
			.HasMaxLength(1000);

		builder
			.Property(p => p.Message)
			.IsRequired()
			.HasMaxLength(200);

		builder
			.Property(p => p.NotifierUserName)
			.HasMaxLength(64);

		builder
			.Property(p => p.Resources)
			.IsRequired()
			.HasMaxLength(500);

		builder
			.HasOne(p => p.AppUser)
			.WithMany(p => p.Notifications)
			.OnDelete(DeleteBehavior.Cascade);
	}

}

