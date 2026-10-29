
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

	public string? Title { get; set; }

	public string Message { get; set; } = string.Empty;

	public DateTime DateCreated { get; set; }

	public string Resources { get; set; } = string.Empty;

	public string? Type { get; set; }

	public bool IsRead { get; set; }

	/// <summary>
	/// When the notification was marked as read. Null while unread.
	/// </summary>
	public DateTimeOffset? ReadAt { get; set; }

	/// <summary>
	/// Optional resource identifier this notification relates to
	/// (e.g. an appointment id or message thread id).
	/// </summary>
	public int? RelatedResourceId { get; set; }

	/// <summary>
	/// Optional caller-supplied key identifying the event a notification came
	/// from. Indexed for lookups only: the index is not unique, so storing a key
	/// does not by itself prevent a second notification for the same event.
	/// </summary>
	public string? DeduplicationKey { get; set; }

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

	public static Notification CreateNotification(string userId, string title, string message, Dictionary<string, int> resource, string notifierUsername, string notifierPhotoUrl, NotificationType type, int? relatedResourceId = null, string? deduplicationKey = null)
	{
		var notification = new Notification()
		{
			AppUserId = userId,
			Title = title,
			DateCreated = DateTime.UtcNow,
			NotifierUserName = notifierUsername,
			NotifierPhotoUrl = notifierPhotoUrl,
			Message = message,
			ResourcesObject = resource,
			TypeEnum = type,
			RelatedResourceId = relatedResourceId,
			DeduplicationKey = deduplicationKey
		};

		return notification;

	}

}
