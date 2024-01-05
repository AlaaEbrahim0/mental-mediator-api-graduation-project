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
        // Possible words for the title
        string[] titleWords = { "Lorem", "Ipsum", "Random", "Post", "Title", "Generator" };

        // Maximum length for the title
        const int maxLength = 255;

        // Random object for generating indices
        Random random = new Random();

        // StringBuilder for efficient string concatenation
        StringBuilder titleBuilder = new StringBuilder();

        // Generate a random title with a random length
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
        // Possible sentences for the body
        string[] bodySentences = { "This is a sample sentence.", "Lorem ipsum dolor sit amet.", "Random body text.", "C# is a powerful language." };

        // Maximum length for the body
        const int maxLength = 2047;

        // Random object for generating indices
        Random random = new Random();

        // StringBuilder for efficient string concatenation
        StringBuilder bodyBuilder = new StringBuilder();

        // Generate a random body with a random length
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
