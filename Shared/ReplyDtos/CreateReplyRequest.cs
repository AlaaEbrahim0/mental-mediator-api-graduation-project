﻿using System.ComponentModel.DataAnnotations;

namespace Shared.ReplyDtos;
public class CreateReplyRequest
{
    [Required(ErrorMessage = "Content is required")]
    [MaxLength(2047, ErrorMessage = "Content cannot exceed 2047 characters")]
    public string? Content { get; set; }
}

public class UpdateReplyRequest
{
    [Required(ErrorMessage = "Content is required")]
    [MaxLength(2047, ErrorMessage = "Content cannot exceed 2047 characters")]
    public string? Content { get; set; }
}
