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

		// A deleted post removes its asset ledger rows; the service destroys the
		// Cloudinary files before the database delete.
		builder
			.HasOne(x => x.Post)
			.WithMany()
			.HasForeignKey(x => x.PostId)
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.OwnerId)
			.OnDelete(DeleteBehavior.NoAction);
	}
}
