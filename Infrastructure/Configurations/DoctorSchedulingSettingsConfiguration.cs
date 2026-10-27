using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

/// <summary>
/// One scheduling-settings row per doctor. A missing row means UTC with a
/// 30-minute consultation.
/// </summary>
public class DoctorSchedulingSettingsConfiguration : IEntityTypeConfiguration<DoctorSchedulingSettings>
{
	public void Configure(EntityTypeBuilder<DoctorSchedulingSettings> builder)
	{
		builder.ToTable("DoctorSchedulingSettings");

		builder.HasKey(x => x.DoctorId);

		builder.Property(x => x.TimeZoneId).HasMaxLength(64);

		builder.Property(x => x.ConsultationDurationMinutes)
			.HasDefaultValue(DoctorSchedulingSettings.DefaultConsultationDurationMinutes);

		builder.ToTable(t => t.HasCheckConstraint(
			"CK_DoctorSchedulingSettings_ConsultationDuration",
			"[ConsultationDurationMinutes] IN (30, 45, 60)"));

		builder.HasOne(x => x.Doctor)
			.WithOne(x => x.SchedulingSettings)
			.HasForeignKey<DoctorSchedulingSettings>(x => x.DoctorId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
