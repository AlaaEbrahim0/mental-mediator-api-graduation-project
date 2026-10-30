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

		// Existing rows predate audiences and are public, so the column can be
		// added to a populated table with a constant default instead of a rewrite.
		builder
			.Property(p => p.Visibility)
			.IsRequired()
			.HasDefaultValue(Domain.Enums.PostVisibility.Public);

		builder
			.Property(p => p.IsDraft)
			.IsRequired()
			.HasDefaultValue(false);

		// A room deletion must not delete published posts. The audience becomes
		// unresolvable instead, and the read policy hides it.
		builder
			.HasOne(p => p.TargetChatRoom)
			.WithMany()
			.HasForeignKey(p => p.TargetChatRoomId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasIndex(p => new { p.Visibility, p.TargetChatRoomId });
		builder.HasIndex(p => new { p.IsDraft, p.PostedOn });

		builder
			.Property(p => p.MediaJson)
			.HasMaxLength(4000);

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
		// Post deletion cascades to likes; disabling the direct user cascade avoids
		// SQL Server's multiple-cascade-path restriction through User -> Post -> Like.
		builder.HasOne<BaseUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
		builder.HasIndex(x => new { x.UserId, x.PostId }).IsUnique();
		builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
	}
}
