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
			.HasIndex(c => c.PostId);

		builder
			.HasIndex(c => c.AppUserId);
	}
}
