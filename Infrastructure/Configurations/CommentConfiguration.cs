using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
	public void Configure(EntityTypeBuilder<Comment> builder)
	{
		builder
			.Property(c => c.Content)
			.HasMaxLength(4000);

		builder
			.Property(c => c.MediaUrl)
			.HasMaxLength(1000);

		builder
			.Property(c => c.MediaType)
			.HasMaxLength(10);

		builder
			.HasIndex(c => c.PostId);

		builder
			.HasIndex(c => c.AppUserId);
	}
}
