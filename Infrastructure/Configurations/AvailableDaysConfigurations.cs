using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AvailableDaysConfigurations : IEntityTypeConfiguration<AvailableDays>
{

	public void Configure(EntityTypeBuilder<AvailableDays> builder)
	{
		builder.ToTable(t => t.HasCheckConstraint("CA_AvailableDays_StartEndTime", "EndTime > StartTime"));
	}

}

