using System.ComponentModel.DataAnnotations;

namespace Shared.CommentsDtos;

public class UpdateCommentRequest
{
    [Required(ErrorMessage = "Content is required")]
    [MaxLength(2047, ErrorMessage = "Content cannot exceed 2047 characters")]
    public string? Content { get; set; }
}
