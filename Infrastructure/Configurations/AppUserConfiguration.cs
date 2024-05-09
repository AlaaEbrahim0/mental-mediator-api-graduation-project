using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<BaseUser>
{

	public void Configure(EntityTypeBuilder<BaseUser> builder)
	{


	}
}
