using Domain.Enums;

namespace Domain.Entities;

public class ChatMessageReport
{
	public int Id { get; set; }

	public long MessageId { get; set; }

	public ChatMessage Message { get; set; } = null!;

	public string ReporterId { get; set; } = string.Empty;

	public string Reason { get; set; } = string.Empty;

	public ChatReportStatus Status { get; set; } = ChatReportStatus.Open;

	public DateTime CreatedAtUtc { get; set; }

	public DateTime? ReviewedAtUtc { get; set; }

	public string? ReviewedById { get; set; }
}
