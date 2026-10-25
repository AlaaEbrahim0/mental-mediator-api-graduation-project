using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ChatMessageReportConfiguration : IEntityTypeConfiguration<ChatMessageReport>
{
	public void Configure(EntityTypeBuilder<ChatMessageReport> builder)
	{
		builder.Property(x => x.ReporterId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
		builder.Property(x => x.ReviewedById).HasMaxLength(450);

		builder
			.HasOne(x => x.Message)
			.WithMany()
			.HasForeignKey(x => x.MessageId)
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.ReporterId)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.ReviewedById)
			.OnDelete(DeleteBehavior.NoAction);

		builder.HasIndex(x => new { x.MessageId, x.ReporterId }).IsUnique();
		builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
	}
}
