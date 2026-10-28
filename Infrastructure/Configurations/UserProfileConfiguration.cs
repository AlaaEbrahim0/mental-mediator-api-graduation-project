using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserCertificateConfiguration : IEntityTypeConfiguration<UserCertificate>
{
	public void Configure(EntityTypeBuilder<UserCertificate> builder)
	{
		builder.ToTable("UserCertificates");

		builder.Property(certificate => certificate.Title)
			.IsRequired()
			.HasMaxLength(160);

		builder.Property(certificate => certificate.IssuingOrganization)
			.IsRequired()
			.HasMaxLength(160);

		builder.Property(certificate => certificate.CredentialId)
			.HasMaxLength(120);

		builder.Property(certificate => certificate.CredentialUrl)
			.HasMaxLength(500);

		builder.Property(certificate => certificate.Description)
			.HasMaxLength(1000);

		builder.Property(certificate => certificate.ImageUrl)
			.HasMaxLength(1000);

		builder.HasOne(certificate => certificate.User)
			.WithMany(user => user.Certificates)
			.HasForeignKey(certificate => certificate.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(certificate => certificate.UserId);
	}
}

public class UserQualificationConfiguration : IEntityTypeConfiguration<UserQualification>
{
	public void Configure(EntityTypeBuilder<UserQualification> builder)
	{
		builder.ToTable("UserQualifications");

		builder.Property(qualification => qualification.Degree)
			.IsRequired()
			.HasMaxLength(160);

		builder.Property(qualification => qualification.Institution)
			.IsRequired()
			.HasMaxLength(200);

		builder.Property(qualification => qualification.FieldOfStudy)
			.HasMaxLength(160);

		builder.Property(qualification => qualification.Description)
			.HasMaxLength(1000);

		builder.HasOne(qualification => qualification.User)
			.WithMany(user => user.Education)
			.HasForeignKey(qualification => qualification.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(qualification => qualification.UserId);
	}
}
