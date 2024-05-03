using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Newtonsoft.Json;

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

	[StringLength(1000, ErrorMessage = "Type length can't be more than 1000.")]
	public string Resources { get; set; } = string.Empty;

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

	[NotMapped]
	public Dictionary<string, int> ResourcesObject
	{
		get
		{
			return JsonConvert.DeserializeObject<Dictionary<string, int>>(Resources)!;
		}
		set
		{
			Resources = JsonConvert.SerializeObject(value);
		}
	}
	public static Notification CreateNotification(string userId, string message, Dictionary<string, int> resource, NotificationType type)
	{
		var notification = new Notification()
		{
			AppUserId = userId,
			DateCreated = DateTime.UtcNow,
			Message = message,
			ResourcesObject = resource,
			TypeEnum = type
		};

		return notification;

	}

}


