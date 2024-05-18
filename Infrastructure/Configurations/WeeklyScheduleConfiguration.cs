using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class WeeklyScheduleConfiguration : IEntityTypeConfiguration<DoctorScheduleWeekDay>
{

	public void Configure(EntityTypeBuilder<DoctorScheduleWeekDay> builder)
	{
		builder.HasKey(x => new { x.DoctorId, x.DayOfWeek });

		builder
			.HasOne(x => x.Doctor)
			.WithMany(x => x.DoctorScheduleWeekDays)
			.HasPrincipalKey(x => x.Id)
			.OnDelete(DeleteBehavior.Cascade);
	}

}

