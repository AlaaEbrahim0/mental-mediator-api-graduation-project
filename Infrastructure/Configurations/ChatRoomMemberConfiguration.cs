using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ChatRoomMemberConfiguration : IEntityTypeConfiguration<ChatRoomMember>
{
	public void Configure(EntityTypeBuilder<ChatRoomMember> builder)
	{
		builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.NoAction);

		builder.HasIndex(x => new { x.RoomId, x.UserId }).IsUnique();
		builder.HasIndex(x => new { x.UserId, x.LeftAtUtc });
	}
}
