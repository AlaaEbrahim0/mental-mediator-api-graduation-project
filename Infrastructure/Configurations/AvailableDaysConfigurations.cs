using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AvailableDaysConfigurations : IEntityTypeConfiguration<AvailableDays>
{

	public void Configure(EntityTypeBuilder<AvailableDays> builder)
	{
		builder
			.HasOne(x => x.WeeklySchedule)
			.WithMany(x => x.AvailableDays)
			.HasForeignKey(x => x.WeeklyScheduleId)
			.OnDelete(DeleteBehavior.Cascade);
	}

}

