using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
	public void Configure(EntityTypeBuilder<Notification> builder)
	{
		builder
			.Property(n => n.Title)
			.HasMaxLength(200);

		builder
			.Property(n => n.Message)
			.HasMaxLength(2000);

		builder
			.Property(n => n.DeduplicationKey)
			.HasMaxLength(200);

		builder
			.HasIndex(n => n.AppUserId);

		builder
			.HasIndex(n => n.IsRead);

		builder
			.HasIndex(n => new { n.AppUserId, n.IsRead });

		builder
			.HasIndex(n => n.DeduplicationKey);

		builder
			.HasIndex(n => new { n.AppUserId, n.Type, n.RelatedResourceId });

		// Only notifications whose event key is expected to be unique are
		// covered. Historical Comment/Reply rows keep their non-unique lookup
		// index because older data may already contain repeated keys.
		builder
			.HasIndex(n => new { n.AppUserId, n.DeduplicationKey })
			.IsUnique()
			.HasFilter("[DeduplicationKey] IS NOT NULL AND [Type] IN ('RoomMessage', 'PostLike', 'FollowUp', 'PatientNote', 'AppointmentCompleted')");
	}
}
