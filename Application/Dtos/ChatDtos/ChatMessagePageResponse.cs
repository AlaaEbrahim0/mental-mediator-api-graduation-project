namespace Application.Dtos.ChatDtos;

public class ChatMessagePageResponse
{
	public List<ChatMessageResponse> Messages { get; set; } = new();

	public bool HasMore { get; set; }

	public long? NextBeforeId { get; set; }

	public long? NextAfterId { get; set; }
}
