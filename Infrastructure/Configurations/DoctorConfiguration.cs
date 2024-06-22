using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{

	public void Configure(EntityTypeBuilder<Doctor> builder)
	{
		builder
			.ToTable("Doctors");

		builder
			.Property(x => x.Biography)
			.HasMaxLength(1000)
			.IsRequired(false);

		builder
			.Property(x => x.City)
			.HasMaxLength(100)
			.IsRequired(false);

		builder
			.Property(x => x.Location)
			.HasMaxLength(200)
			.IsRequired(false);


		builder
			.HasMany(d => d.Appointments)
			.WithOne(d => d.Doctor)
			.OnDelete(DeleteBehavior.NoAction);
	}

}

