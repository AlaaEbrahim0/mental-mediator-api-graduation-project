using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DoctorRatingConfiguration : IEntityTypeConfiguration<DoctorRating>
{
	public void Configure(EntityTypeBuilder<DoctorRating> builder)
	{
		builder.Property(x => x.DoctorId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.RaterUserId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
		builder.Property(x => x.Comment).HasMaxLength(1000);

		// Accounts are soft-deleted and doctors are hidden by filter, never
		// physically removed, so NoAction keeps FKs simple and safe.
		builder
			.HasOne<Doctor>()
			.WithMany()
			.HasForeignKey(x => x.DoctorId)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.RaterUserId)
			.OnDelete(DeleteBehavior.NoAction);

		builder.HasIndex(x => new { x.DoctorId, x.RaterUserId }).IsUnique();
	}
}
