using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ReplyConfiguration : IEntityTypeConfiguration<Reply>
{

    public void Configure(EntityTypeBuilder<Reply> builder)
    {

    }
}
