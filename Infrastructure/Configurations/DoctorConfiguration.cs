using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{

	public void Configure(EntityTypeBuilder<Doctor> builder)
	{
		builder
			.ToTable("Doctors")
			.HasMany(d => d.Appointments)
			.WithOne(d => d.Doctor)
			.OnDelete(DeleteBehavior.SetNull);
	}

}

