using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;
public class BaseUser : IdentityUser
{
	public string FirstName { get; set; } = null!;
	public string LastName { get; set; } = null!;
	public DateOnly BirthDate { get; set; }
	public string Gender { get; set; } = null!;

	[NotMapped]
	public string FullName => $"{FirstName} {LastName}";

	public override string NormalizedEmail { get => base.Email!.ToUpper(); }
	public override string NormalizedUserName { get => base.UserName!.ToUpper(); }

	public string? PhotoUrl { get; set; }

	public override string UserName => Email!.Split('@')[0];

	public List<Post> Posts { get; set; } = new();
	public List<Notification> Notifications { get; set; } = new();
}

