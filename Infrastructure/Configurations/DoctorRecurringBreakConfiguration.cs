using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DoctorRecurringBreakConfiguration : IEntityTypeConfiguration<DoctorRecurringBreak>
{
	public void Configure(EntityTypeBuilder<DoctorRecurringBreak> builder)
	{
		builder.ToTable("DoctorRecurringBreaks");
		builder.Property(x => x.Name).HasMaxLength(200);
		builder.HasIndex(x => new { x.DoctorId, x.DayOfWeek, x.StartTime, x.EndTime });
		builder.HasOne(x => x.Doctor)
			.WithMany(x => x.RecurringBreaks)
			.HasForeignKey(x => x.DoctorId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
