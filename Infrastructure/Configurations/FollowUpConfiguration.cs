using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class FollowUpConfiguration : IEntityTypeConfiguration<FollowUp>
{
	public void Configure(EntityTypeBuilder<FollowUp> builder)
	{
		builder.Property(x => x.DoctorId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.PatientId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.Reason).HasMaxLength(200).IsRequired();

		// Same reason as PatientNoteConfiguration: no navigation to a filtered
		// BaseUser, or a deleted patient would hide the follow-up everywhere.
		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.DoctorId)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne<BaseUser>()
			.WithMany()
			.HasForeignKey(x => x.PatientId)
			.OnDelete(DeleteBehavior.NoAction);

		builder
			.HasOne<Appointment>()
			.WithMany()
			.HasForeignKey(x => x.AppointmentId)
			.OnDelete(DeleteBehavior.NoAction);

		builder.HasIndex(x => new { x.DoctorId, x.Status, x.DueAtUtc });
		builder.HasIndex(x => new { x.PatientId, x.Status });
	}
}
