using System.Text;
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
        builder
            .HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .OnDelete(DeleteBehavior.Cascade);

        bool migrationsApplied = builder.Metadata.GetChangeTrackingStrategy() != ChangeTrackingStrategy.Snapshot;

        if (!migrationsApplied)
        {
            int id = 1;
            var postFaker = new Faker<Post>()
            .RuleFor(x => x.Id, set => id++)
            .RuleFor(x => x.PostedOn, set => set.Date.Past())
            .RuleFor(x => x.Title, GenerateRandomPostTitle())
            .RuleFor(x => x.Content, GenerateRandomPostBody())
            .RuleFor(x => x.AppUserId, set => set.Random.ArrayElement(ids));

            var posts = postFaker.Generate(200);
            builder.HasData(posts);
        }
    }

    private string GenerateRandomPostTitle()
    {
        string[] titleWords = { "Lorem", "Ipsum", "Random", "Post", "Title", "Generator" };

        const int maxLength = 255;

        Random random = new Random();

        StringBuilder titleBuilder = new StringBuilder();

        while (titleBuilder.Length < maxLength)
        {
            string randomWord = titleWords[random.Next(titleWords.Length)];
            if (titleBuilder.Length + randomWord.Length <= maxLength)
            {
                titleBuilder.Append(randomWord).Append(' ');
            }
            else
            {
                break;
            }
        }

        return titleBuilder.ToString().Trim();
    }

    private string GenerateRandomPostBody()
    {
        string[] bodySentences = { "This is a sample sentence.", "Lorem ipsum dolor sit amet.", "Random body text." };

        const int maxLength = 2047;

        Random random = new Random();

        StringBuilder bodyBuilder = new StringBuilder();

        while (bodyBuilder.Length < maxLength)
        {
            string randomSentence = bodySentences[random.Next(bodySentences.Length)];
            if (bodyBuilder.Length + randomSentence.Length <= maxLength)
            {
                bodyBuilder.Append(randomSentence).Append(' ');
            }
            else
            {
                break;
            }
        }

        return bodyBuilder.ToString().Trim();
    }

}
