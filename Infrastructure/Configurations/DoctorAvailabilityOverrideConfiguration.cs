using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DoctorAvailabilityOverrideConfiguration : IEntityTypeConfiguration<DoctorAvailabilityOverride>
{
	public void Configure(EntityTypeBuilder<DoctorAvailabilityOverride> builder)
	{
		builder.ToTable("DoctorAvailabilityOverrides");
		builder.Property(x => x.PrivateReason).HasMaxLength(500);
		builder.HasIndex(x => new { x.DoctorId, x.Date, x.Type, x.IsActive });
		builder.HasOne(x => x.Doctor)
			.WithMany(x => x.AvailabilityOverrides)
			.HasForeignKey(x => x.DoctorId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
