using Bogus;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ReplyConfiguration : IEntityTypeConfiguration<Reply>
{
    private readonly string[] ids;

    public ReplyConfiguration(string[] ids)
    {
        this.ids = ids;
    }

    public void Configure(EntityTypeBuilder<Reply> builder)
    {
        bool migrationsApplied = builder.Metadata.GetChangeTrackingStrategy() != ChangeTrackingStrategy.Snapshot;

        if (!migrationsApplied)
        {
            var commentIds = Enumerable.Range(1, 300).ToArray();

            int id = 1;
            var commentFaker = new Faker<Reply>()
                .RuleFor(c => c.Id, set => id++)
                .RuleFor(c => c.AppUserId, set => set.Random.ArrayElement(ids))
                .RuleFor(c => c.RepliedAt, set => set.Date.Past())
                .RuleFor(c => c.Content, set => set.Lorem.Sentence())
                .RuleFor(c => c.CommentId, set => set.Random.ArrayElement(commentIds));

            var comments = commentFaker.Generate(750);
            builder.HasData(comments);
        }
    }
}
