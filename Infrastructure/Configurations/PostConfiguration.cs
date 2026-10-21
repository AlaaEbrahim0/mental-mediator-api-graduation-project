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

		builder.HasMany(p => p.Likes).WithOne(l => l.Post).HasForeignKey(l => l.PostId).OnDelete(DeleteBehavior.Cascade);

		builder.Property(p => p.Category).HasMaxLength(100);
		builder.Property(p => p.Tags).HasMaxLength(1000);

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

public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
	public void Configure(EntityTypeBuilder<PostLike> builder)
	{
		builder.HasKey(x => x.Id);
		builder.HasOne<BaseUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
		builder.HasIndex(x => new { x.UserId, x.PostId }).IsUnique();
		builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
	}
}
