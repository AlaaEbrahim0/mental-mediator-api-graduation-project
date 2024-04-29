using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;

namespace Domain.Entities;

public class Notification
{
	public int Id { get; set; }

	public string? AppUserId { get; set; }

	public AppUser? AppUser { get; set; }

	[Required]
	[StringLength(500, ErrorMessage = "Message length can't be more than 500.")]
	public string? Message { get; set; }

	[Required]
	public DateTime DateCreated { get; set; }

	public bool IsRead { get; set; }

	[Required]
	public int ResourceId { get; set; }

	[Required]
	[StringLength(100, ErrorMessage = "Type length can't be more than 20.")]
	public string? Type { get; set; }

	[NotMapped]
	public NotificationType TypeEnum
	{
		get
		{
			return Enum.Parse<NotificationType>(Type!);
		}
		set
		{
			Type = Enum.GetName(value);
		}
	}

	public static Notification CreateNotification(string message, NotificationType type, string userId, int resourceId)
	{
		return new Notification
		{
			Message = message,
			TypeEnum = type,
			DateCreated = DateTime.UtcNow,
			AppUserId = userId,
			ResourceId = resourceId
		};
	}
}



