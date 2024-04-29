namespace Shared;
public record NotificationResponse
{
	public int Id { get; set; }

	public string Title { get; set; } = string.Empty;

	public string? Message { get; set; }

	public DateTime DateCreated { get; set; }

	public bool IsRead { get; set; }

	public int ResourceId { get; set; }

	public string? Type { get; set; }
}
