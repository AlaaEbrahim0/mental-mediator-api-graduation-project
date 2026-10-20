using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
	public void Configure(EntityTypeBuilder<Notification> builder)
	{
		builder
			.Property(n => n.Message)
			.HasMaxLength(2000);

		builder
			.HasIndex(n => n.AppUserId);

		builder
			.HasIndex(n => n.IsRead);

		builder
			.HasIndex(n => new { n.AppUserId, n.IsRead });
	}
}
