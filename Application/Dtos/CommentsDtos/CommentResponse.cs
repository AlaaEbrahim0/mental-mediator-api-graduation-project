﻿
namespace Application.Dtos.CommentsDtos;

public class CommentResponse
{
	public int Id { get; set; }
	public string? Content { get; set; }
	public DateTime CommentedAt { get; set; }
	public string? Username { get; set; }
	public string? AppUserId { get; set; }
	public string? PhotoUrl { get; set; }
	public int RepliesCount { get; set; }

}