using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{

	public void Configure(EntityTypeBuilder<Comment> builder)
	{

		builder
			.Property(x => x.Content)
			.IsRequired()
			.HasMaxLength(1000);

		builder
			.HasMany(c => c.Replies)
			.WithOne(c => c.Comment)
			.OnDelete(DeleteBehavior.Cascade);

	}
}
