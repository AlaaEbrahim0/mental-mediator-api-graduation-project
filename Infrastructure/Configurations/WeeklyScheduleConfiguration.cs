using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class WeeklyScheduleConfiguration : IEntityTypeConfiguration<WeeklySchedule>
{

	public void Configure(EntityTypeBuilder<WeeklySchedule> builder)
	{
		builder
			.HasMany(d => d.AvailableDays)
			.WithOne()
			.HasPrincipalKey(x => x.Id)
			.OnDelete(DeleteBehavior.Cascade);

	}

}

