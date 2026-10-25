using Domain.Enums;

namespace Application.Dtos.ChatDtos;

public class ChatMessageReportResponse
{
	public int Id { get; set; }

	public long MessageId { get; set; }

	public string Reason { get; set; } = string.Empty;

	public ChatReportStatus Status { get; set; }

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? ReviewedAtUtc { get; set; }

	public ChatMessageResponse Message { get; set; } = null!;
}
