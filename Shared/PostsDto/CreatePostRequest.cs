using System.ComponentModel.DataAnnotations;

namespace Shared.PostsDto;
public class CreatePostRequest
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
    public string? Title { get; set; }


    [Required(ErrorMessage = "Content is required")]
    [MaxLength(2047, ErrorMessage = "Content cannot exceed 2047 characters")]
    public string? Content { get; set; }

    [Required]
    public bool IsAnonymous { get; set; }
}
