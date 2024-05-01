namespace Shared;
public record NotificationResponse
{
	public int Id { get; set; }

	public string Message { get; set; } = string.Empty;

	public DateTime DateCreated { get; set; }

	public bool IsRead { get; set; }

	public int ResourceId { get; set; }

	public string? Type { get; set; }
}
