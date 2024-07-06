using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class DepressionTestResultConfiguration : IEntityTypeConfiguration<DepressionTestResult>
{
	public void Configure(EntityTypeBuilder<DepressionTestResult> builder)
	{
		builder
			.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.IsRequired(false)
			.OnDelete(DeleteBehavior.SetNull);

		builder
			.Property(x => x.Result)
			.HasMaxLength(100);
	}
}