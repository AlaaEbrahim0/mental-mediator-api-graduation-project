using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CommentReactionConfiguration : IEntityTypeConfiguration<CommentReaction>
{
	public void Configure(EntityTypeBuilder<CommentReaction> builder)
	{
		builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.Reaction).HasMaxLength(16).IsRequired();

		builder.HasIndex(x => new { x.UserId, x.CommentId }).IsUnique();
		builder.HasIndex(x => x.CommentId);

		builder
			.HasOne(x => x.Comment)
			.WithMany(x => x.Reactions)
			.HasForeignKey(x => x.CommentId)
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.NoAction);
	}
}

public class ReplyReactionConfiguration : IEntityTypeConfiguration<ReplyReaction>
{
	public void Configure(EntityTypeBuilder<ReplyReaction> builder)
	{
		builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.Reaction).HasMaxLength(16).IsRequired();

		builder.HasIndex(x => new { x.UserId, x.ReplyId }).IsUnique();
		builder.HasIndex(x => x.ReplyId);

		builder
			.HasOne(x => x.Reply)
			.WithMany(x => x.Reactions)
			.HasForeignKey(x => x.ReplyId)
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.NoAction);
	}
}

public class ChatMessageReactionConfiguration : IEntityTypeConfiguration<ChatMessageReaction>
{
	public void Configure(EntityTypeBuilder<ChatMessageReaction> builder)
	{
		builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.Reaction).HasMaxLength(16).IsRequired();

		builder.HasIndex(x => new { x.UserId, x.MessageId }).IsUnique();
		builder.HasIndex(x => x.MessageId);

		builder
			.HasOne(x => x.Message)
			.WithMany(x => x.Reactions)
			.HasForeignKey(x => x.MessageId)
			.OnDelete(DeleteBehavior.Cascade);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.NoAction);
	}
}
