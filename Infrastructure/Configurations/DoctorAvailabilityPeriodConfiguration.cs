using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

/// <summary>
/// One row per weekly availability period. The key is a surrogate id rather
/// than (DoctorId, DayOfWeek) so a weekday can have several periods.
/// </summary>
public class DoctorAvailabilityPeriodConfiguration : IEntityTypeConfiguration<DoctorAvailabilityPeriod>
{
	public void Configure(EntityTypeBuilder<DoctorAvailabilityPeriod> builder)
	{
		builder.ToTable("DoctorAvailabilityPeriods");

		builder.HasKey(x => x.Id);

		builder.HasIndex(x => new { x.DoctorId, x.DayOfWeek, x.StartTime });

		builder.HasOne(x => x.Doctor)
			.WithMany(x => x.AvailabilityPeriods)
			.HasForeignKey(x => x.DoctorId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
