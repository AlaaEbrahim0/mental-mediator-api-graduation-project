using Bogus;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    private readonly string[] ids;

    public PostConfiguration(string[] ids)
    {
        this.ids = ids;
    }
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        int id = 1;
        var postFaker = new Faker<Post>()       
            .RuleFor(x => x.Id, set => id++)
            .RuleFor(x => x.PostedOn, set => set.Date.Past())
            .RuleFor(x => x.Title, set => set.Lorem.Text())
            .RuleFor(x => x.AppUserId, set => set.Random.ArrayElement(ids));

        var posts = postFaker.Generate(100);
        builder.HasData(posts);
    }
}
