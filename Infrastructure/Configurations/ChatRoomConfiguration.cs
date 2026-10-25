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
	}
}
