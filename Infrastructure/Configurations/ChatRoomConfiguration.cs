using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
{
	public void Configure(EntityTypeBuilder<ChatRoom> builder)
	{
		builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
		builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
		builder.Property(x => x.CreatedById).HasMaxLength(450).IsRequired();
		builder.Property(x => x.PasswordHash).HasMaxLength(200);
		builder.Property(x => x.CoverImageUrl).HasMaxLength(1000);

		// A constant default keeps the column additive on a populated table: SQL
		// Server fills existing rows without a rewrite of any other column.
		builder.Property(x => x.IsLocked).IsRequired().HasDefaultValue(false);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.CreatedById)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasMany(x => x.Members)
			.WithOne(x => x.Room)
			.HasForeignKey(x => x.RoomId)
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.HasMany(x => x.Messages)
			.WithOne(x => x.Room)
			.HasForeignKey(x => x.RoomId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.Visibility, x.IsArchived, x.LastMessageAtUtc });

		// Private rooms become listable, so visibility is no longer a filter in the
		// directory query; archived/recency is what every browse path orders by.
		builder.HasIndex(x => new { x.IsArchived, x.IsLocked, x.LastMessageAtUtc });

		builder
			.HasIndex(x => x.ScheduledStartAtUtc)
			.HasFilter("[ScheduledStartAtUtc] IS NOT NULL");
	}
}
