using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DoctorTimeOffConfiguration : IEntityTypeConfiguration<DoctorTimeOff>
{
	public void Configure(EntityTypeBuilder<DoctorTimeOff> builder)
	{
		builder.ToTable("DoctorTimeOff");

		builder.Property(x => x.Reason).HasMaxLength(500);

		builder.HasIndex(x => new { x.DoctorId, x.StartTimeUtc, x.EndTimeUtc });

		builder.HasOne(x => x.Doctor)
			.WithMany(x => x.TimeOff)
			.HasForeignKey(x => x.DoctorId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
