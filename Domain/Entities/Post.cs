namespace Domain.Entities;

public class Post
{
    public int Id { get; set; }
    public string? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public string? Title { get; set; }
    public DateTime PostedOn { get; set; }

    public List<Comment> Comments { get; set; } = new();
}
