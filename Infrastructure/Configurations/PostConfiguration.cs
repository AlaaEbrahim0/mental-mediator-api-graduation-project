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
			.Property(p => p.Title)
			.HasMaxLength(2000);

		builder
			.Property(p => p.Content)
			.HasMaxLength(10000);

		// Performance indexes
		builder
			.HasIndex(p => p.PostedOn);

		builder
			.HasIndex(p => p.AppUserId);

		builder
			.HasIndex(p => p.IsAnonymous);
	}
}
