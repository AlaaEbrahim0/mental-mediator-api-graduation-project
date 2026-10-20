using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ReplyConfiguration : IEntityTypeConfiguration<Reply>
{
	public void Configure(EntityTypeBuilder<Reply> builder)
	{
		builder
			.Property(r => r.Content)
			.HasMaxLength(4000);

		builder
			.HasIndex(r => r.CommentId);

		builder
			.HasIndex(r => r.AppUserId);
	}
}
