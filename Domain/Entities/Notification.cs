using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Notification
{
	public int Id { get; set; }

	public string? AppUserId { get; set; }

	public AppUser? AppUser { get; set; }

	[Required]
	[StringLength(100, ErrorMessage = "Title length can't be more than 100.")]
	public string? Title { get; set; }

	[Required]
	[StringLength(500, ErrorMessage = "Message length can't be more than 500.")]
	public string? Message { get; set; }

	[Required]
	public DateTime DateCreated { get; set; }

	public bool IsRead { get; set; }
}



