using Application.Dtos.ReactionsDto;

namespace Application.Dtos.ReplyDtos;

public class ReplyResponse
{
	public int Id { get; set; }
	public string? Content { get; set; }
	public DateTime RepliedAt { get; set; }
	public string? Username { get; set; }
	public string? AppUserId { get; set; }
	public string? PhotoUrl { get; set; }

	public string? MediaUrl { get; set; }
	public string? MediaType { get; set; }

	public List<ReactionSummaryResponse> Reactions { get; set; } = new();
	public string? MyReaction { get; set; }
}
