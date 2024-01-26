using Bogus;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    private readonly string[] ids;

    public CommentConfiguration(string[] ids)
    {
        this.ids = ids;
    }

    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder
            .HasMany(c => c.Replies)
            .WithOne(c => c.Comment)
            .OnDelete(DeleteBehavior.Cascade);

        bool migrationsApplied = builder.Metadata.GetChangeTrackingStrategy() != ChangeTrackingStrategy.Snapshot;

        if (!migrationsApplied)
        {
            var postIds = Enumerable.Range(1, 100).ToArray();

            int id = 1;
            var commentFaker = new Faker<Comment>()
                .RuleFor(c => c.Id, set => id++)
                .RuleFor(c => c.AppUserId, set => set.Random.ArrayElement(ids))
                .RuleFor(c => c.CommentedAt, set => set.Date.Past())
                .RuleFor(c => c.Content, set => set.Lorem.Sentence())
                .RuleFor(c => c.PostId, set => set.Random.ArrayElement(postIds));


            var comments = commentFaker.Generate(1000);
            builder.HasData(comments);
        }
    }
}
