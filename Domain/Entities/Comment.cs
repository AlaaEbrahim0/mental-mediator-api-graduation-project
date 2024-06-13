﻿using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;
public class Comment
{
	public int Id { get; set; }
	public int PostId { get; set; }
	public string? AppUserId { get; set; }
	public Post Post { get; set; } = null!;
	public BaseUser AppUser { get; set; } = null!;

	public string Content { get; set; } = null!;
	public DateTime CommentedAt { get; set; }

	public List<Reply> Replies { get; set; } = new();

	[NotMapped]
	public string? PhotoUrl { get; set; }

	[NotMapped]
	public string? Username { get; set; }

	[NotMapped]
	public int RepliesCount { get; set; }

}

