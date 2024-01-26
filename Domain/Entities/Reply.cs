using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class Reply
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public string? AppUserId { get; set; }
    public string? Content { get; set; }
    public DateTime RepliedAt { get; set; }

    public AppUser? AppUser { get; set; }
    public Comment? Comment { get; set; }

    [NotMapped]
    public string? Username { get; set; }
}