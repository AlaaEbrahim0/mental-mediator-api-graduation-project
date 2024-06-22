
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Newtonsoft.Json;

namespace Domain.Entities;


public class Notification
{
	public int Id { get; set; }

	public string AppUserId { get; set; } = string.Empty;

	public BaseUser? AppUser { get; set; }

	public string? NotifierUserName { get; set; }

	public string? NotifierPhotoUrl { get; set; }

	public string Message { get; set; } = string.Empty;

	public DateTime DateCreated { get; set; }

	public string Resources { get; set; } = string.Empty;

	public string? Type { get; set; }

	public bool IsRead { get; set; }

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

	public static Notification CreateNotification(string userId, string message, Dictionary<string, int> resource, string notifierUsername, string notifierPhotoUrl, NotificationType type)
	{
		var notification = new Notification()
		{
			AppUserId = userId,
			DateCreated = DateTime.UtcNow,
			NotifierUserName = notifierUsername,
			NotifierPhotoUrl = notifierPhotoUrl,
			Message = message,
			ResourcesObject = resource,
			TypeEnum = type
		};

		return notification;

	}

}
