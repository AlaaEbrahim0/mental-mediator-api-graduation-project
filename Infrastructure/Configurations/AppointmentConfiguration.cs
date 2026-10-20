using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
	public void Configure(EntityTypeBuilder<Appointment> builder)
	{
		builder
			.HasOne(x => x.Doctor)
			.WithMany(x => x.Appointments);

		builder
			.HasOne(x => x.User)
			.WithMany(x => x.Appointments);

		builder
			.Property(x => x.CancellationReason)
			.HasMaxLength(1000);

		builder
			.Property(x => x.Reason)
			.HasMaxLength(1000);

		builder
			.Property(x => x.RejectionReason)
			.HasMaxLength(1000);

		// Performance indexes
		builder
			.HasIndex(x => x.StartTime);

		builder
			.HasIndex(x => x.DoctorId);

		builder
			.HasIndex(x => x.UserId);

		builder
			.HasIndex(x => x.Status);

		builder
			.HasIndex(x => new { x.DoctorId, x.StartTime, x.Status });
	}
}
