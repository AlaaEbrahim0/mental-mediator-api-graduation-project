namespace Shared.ReplyDtos;
public class ReplyResponse
{
    public int Id { get; set; }
    public string? Content { get; set; }
    public DateTime RepliedAt { get; set; }
    public string? Username { get; set; }
    public string? AppUserId { get; set; }
}
