using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

/// <summary>
/// One scheduling-settings row per doctor. The relationship is keyed on the
/// doctor id rather than a surrogate key so the lookup in the availability
/// calculation stays a single indexed read.
/// </summary>
public class DoctorSchedulingSettingsConfiguration : IEntityTypeConfiguration<DoctorSchedulingSettings>
{
	public void Configure(EntityTypeBuilder<DoctorSchedulingSettings> builder)
	{
		builder.ToTable("DoctorSchedulingSettings");

		builder.HasKey(x => x.DoctorId);

		builder.Property(x => x.TimeZoneId).HasMaxLength(64);

		builder.HasOne(x => x.Doctor)
			.WithOne(x => x.SchedulingSettings)
			.HasForeignKey<DoctorSchedulingSettings>(x => x.DoctorId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
