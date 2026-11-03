using Application.Dtos.ReactionsDto;

namespace Application.Dtos.ChatDtos;

/// <summary>
/// A neutral reaction update broadcast to a room. It deliberately carries the
/// actor's membership id instead of a per-viewer "my reaction" flag, because
/// one SignalR payload is delivered to every member.
/// </summary>
public class ChatMessageReactionsUpdatedEvent
{
	public long MessageId { get; set; }

	public int RoomId { get; set; }

	public List<ReactionSummaryResponse> Reactions { get; set; } = new();

	public int ActorMemberId { get; set; }

	public string? ActorReaction { get; set; }
}
