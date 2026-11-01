using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ChatRoomInvitationConfiguration : IEntityTypeConfiguration<ChatRoomInvitation>
{
	public void Configure(EntityTypeBuilder<ChatRoomInvitation> builder)
	{
		builder.Property(x => x.InvitedUserId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.InvitedById).HasMaxLength(450).IsRequired();

		builder
			.HasOne(x => x.Room)
			.WithMany()
			.HasForeignKey(x => x.RoomId)
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.HasOne(x => x.InvitedUser)
			.WithMany()
			.HasForeignKey(x => x.InvitedUserId)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne(x => x.InvitedBy)
			.WithMany()
			.HasForeignKey(x => x.InvitedById)
			.OnDelete(DeleteBehavior.NoAction);

		builder.HasIndex(x => new { x.InvitedUserId, x.Status });

		// At most one pending invitation per (room, user): the database, not a
		// read-then-write in the service, is the guard against a double invite.
		builder
			.HasIndex(x => new { x.RoomId, x.InvitedUserId })
			.IsUnique()
			.HasFilter($"[Status] = {(int)InvitationStatus.Pending}");
	}
}
