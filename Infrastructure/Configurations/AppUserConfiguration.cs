using Bogus;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    private readonly string[] ids;

    public AppUserConfiguration(string[] ids)
    {
        this.ids = ids;
    }

    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        bool migrationsApplied = builder.Metadata.GetChangeTrackingStrategy() != ChangeTrackingStrategy.Snapshot;

        if (!migrationsApplied)
        {
            int id = 0;
            var userFaker = new Faker<AppUser>()
                .RuleFor(u => u.Id, set => ids[id++])
                .RuleFor(u => u.FirstName, set => set.Person.FirstName)
                .RuleFor(u => u.LastName, set => set.Person.LastName)
                .RuleFor(u => u.BirthDate, set => set.Date.BetweenDateOnly(new DateOnly(1950, 1, 1), new DateOnly(2003, 1, 1)))
                .RuleFor(u => u.Email, set => set.Person.Email)
                .RuleFor(u => u.Gender, set => set.Random.Enum<Gender>().ToString())
                .RuleFor(u => u.PasswordHash, set => set.Internet.Password(10));

            var users = userFaker.Generate(50);
            builder.HasData(users);
        }
    }
}
