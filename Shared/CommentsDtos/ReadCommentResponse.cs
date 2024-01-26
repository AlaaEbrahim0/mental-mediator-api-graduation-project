namespace Domain.Entities;

public class ReadCommentResponse
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public DateTime CommentedAt { get; set; }
    public string? Username { get; set; }
}