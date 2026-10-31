using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PatientNoteConfiguration : IEntityTypeConfiguration<PatientNote>
{
	public void Configure(EntityTypeBuilder<PatientNote> builder)
	{
		builder.Property(x => x.DoctorId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.PatientId).HasMaxLength(450).IsRequired();
		builder.Property(x => x.Title).HasMaxLength(120).IsRequired();
		builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();

		// Deliberately no BaseUser navigation property. AppUserConfiguration puts a
		// soft-delete query filter on BaseUser, EF applies query filters to reference
		// navigations, and a deleted author would silently erase the note from every
		// join. Display names are resolved with a batched projection instead.
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

		builder.HasIndex(x => new { x.DoctorId, x.CreatedAtUtc });
		builder.HasIndex(x => new { x.PatientId, x.CreatedAtUtc });
		builder.Property(x => x.IsPatientVisible).HasDefaultValue(false);
		builder.HasIndex(x => new { x.PatientId, x.IsPatientVisible });
	}
}
