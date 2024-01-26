namespace Domain.Entities;

public class CommentResponse
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public DateTime CommentedAt { get; set; }
    public string? Username { get; set; }
}