using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DoctorTimeBlockConfiguration : IEntityTypeConfiguration<DoctorTimeBlock>
{
	public void Configure(EntityTypeBuilder<DoctorTimeBlock> builder)
	{
		builder.ToTable("DoctorTimeBlocks");
		builder.Property(x => x.PrivateReason).HasMaxLength(500);
		builder.HasIndex(x => new { x.DoctorId, x.StartTimeUtc, x.EndTimeUtc, x.IsCancelled });
		builder.HasOne(x => x.Doctor)
			.WithMany(x => x.TimeBlocks)
			.HasForeignKey(x => x.DoctorId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
