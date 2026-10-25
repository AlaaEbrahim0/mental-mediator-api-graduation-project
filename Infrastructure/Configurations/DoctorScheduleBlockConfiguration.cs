using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DoctorScheduleBlockConfiguration : IEntityTypeConfiguration<DoctorScheduleBlock>
{
	public void Configure(EntityTypeBuilder<DoctorScheduleBlock> builder)
	{
		builder
			.Property(x => x.Reason)
			.HasMaxLength(500);

		// Booking validation and slot calculation filter blocks per doctor and
		// start time, so that pair is the one the queries narrow on.
		builder
			.HasIndex(x => new { x.DoctorId, x.StartTime });

		builder
			.HasOne(x => x.Doctor)
			.WithMany()
			.HasPrincipalKey(x => x.Id)
			.OnDelete(DeleteBehavior.Cascade);
	}
}