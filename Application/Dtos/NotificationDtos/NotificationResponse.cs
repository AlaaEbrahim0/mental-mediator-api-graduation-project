
namespace Application.Dtos.NotificationDtos;
public record NotificationResponse
{
	public int Id { get; set; }

	public string? Title { get; set; }

	public string Message { get; set; } = string.Empty;

	public DateTime DateCreated { get; set; }

	public bool IsRead { get; set; }

	public DateTimeOffset? ReadAt { get; set; }

	public string? NotifierUserName { get; set; }

	public string? NotifierPhotoUrl { get; set; }

	public Dictionary<string, int> Resources { get; set; } = new();

	public string? Type { get; set; }

	public int? RelatedResourceId { get; set; }

	public int RelatedCount { get; set; } = 1;
}
