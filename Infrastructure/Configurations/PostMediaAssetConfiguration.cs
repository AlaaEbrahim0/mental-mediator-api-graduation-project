using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PostMediaAssetConfiguration : IEntityTypeConfiguration<PostMediaAsset>
{
	public void Configure(EntityTypeBuilder<PostMediaAsset> builder)
	{
		builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
		builder.Property(x => x.PublicId).HasMaxLength(500).IsRequired();
		builder.Property(x => x.MediaType).HasMaxLength(16).IsRequired();
		builder.Property(x => x.OwnerId).HasMaxLength(450).IsRequired();

		builder.HasIndex(x => x.Url).IsUnique();
		builder.HasIndex(x => new { x.OwnerId, x.PostId, x.CreatedAtUtc });
		builder.HasIndex(x => new { x.OwnerId, x.CommentId, x.CreatedAtUtc });
		builder.HasIndex(x => new { x.OwnerId, x.ReplyId, x.CreatedAtUtc });

		// A deleted post removes its asset ledger rows; the service destroys the
		// Cloudinary files before the database delete.
		builder
			.HasOne(x => x.Post)
			.WithMany()
			.HasForeignKey(x => x.PostId)
			.OnDelete(DeleteBehavior.Cascade);

		// Comment/reply attachments are cleaned up explicitly by their services
		// before the row is deleted. Cascading here would create a second cascade
		// path from Post to PostMediaAsset through Comment.
		builder
			.HasOne(x => x.Comment)
			.WithMany()
			.HasForeignKey(x => x.CommentId)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne(x => x.Reply)
			.WithMany()
			.HasForeignKey(x => x.ReplyId)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.OwnerId)
			.OnDelete(DeleteBehavior.NoAction);
	}
}
