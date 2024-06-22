using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ReplyConfiguration : IEntityTypeConfiguration<Reply>
{

	public void Configure(EntityTypeBuilder<Reply> builder)
	{
		builder
			.Property(x => x.Content)
			.IsRequired()
			.HasMaxLength(1000);
	}
}
