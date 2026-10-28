using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
	public void Configure(EntityTypeBuilder<Doctor> builder)
	{
		builder.ToTable("Doctors");
		builder.Property(x => x.Qualifications).HasMaxLength(2000);
		builder.Property(x => x.ProfessionalTitle).HasMaxLength(120);
		builder.Property(x => x.ClinicName).HasMaxLength(200);
		builder.Property(x => x.IsAvailable).HasDefaultValue(true);
		builder.Property(x => x.SessionFees).HasPrecision(18, 2);
		builder.Property(x => x.City).HasMaxLength(100).IsRequired(false);
		builder.Property(x => x.Location).HasMaxLength(200).IsRequired(false);
		builder.HasMany(d => d.Appointments).WithOne(d => d.Doctor).OnDelete(DeleteBehavior.NoAction);
		builder.HasIndex(d => d.Specialization);
		builder.HasIndex(d => d.City);
	}
}
