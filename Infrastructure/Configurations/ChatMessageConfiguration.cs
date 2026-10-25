using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
	public void Configure(EntityTypeBuilder<ChatMessage> builder)
	{
		builder.Property(x => x.SenderId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.SenderDisplayName).HasMaxLength(100).IsRequired();
		builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
		builder.Property(x => x.DeletedById).HasMaxLength(450);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.SenderId)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.DeletedById)
			.OnDelete(DeleteBehavior.NoAction);

		builder.HasIndex(x => new { x.SenderId, x.ClientMessageId }).IsUnique();
		builder.HasIndex(x => new { x.RoomId, x.Id });
	}
}
