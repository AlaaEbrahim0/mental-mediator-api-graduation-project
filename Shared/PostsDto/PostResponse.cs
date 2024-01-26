using System.ComponentModel.DataAnnotations;

namespace Shared.PostsDto;

public class PostResponse
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Content is required")]
    [MaxLength(2047, ErrorMessage = "Title cannot exceed 2048 characters")]
    public string Content { get; set; } = string.Empty;
    public DateTime PostedOn { get; set; }

    public string Username { get; set; } = string.Empty;
}


