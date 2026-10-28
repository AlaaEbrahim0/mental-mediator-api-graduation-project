using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;
public class BaseUser : IdentityUser
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public DateOnly BirthDate { get; set; }
	public string Gender { get; set; } = string.Empty;
	public string? Biography { get; set; }
	public string? City { get; set; }

	/// <summary>Short professional headline shown under the display name.</summary>
	public string? Headline { get; set; }

	public string? Pronouns { get; set; }

	public string? CoverPhotoUrl { get; set; }

	public string? WebsiteUrl { get; set; }
	public string? LinkedInUrl { get; set; }
	public string? TwitterUrl { get; set; }
	public string? InstagramUrl { get; set; }
	public string? FacebookUrl { get; set; }
	public string? YouTubeUrl { get; set; }

	/// <summary>Comma-separated list, e.g. "Arabic, English, French".</summary>
	public string? Languages { get; set; }

	/// <summary>Comma-separated interests shown on the public profile.</summary>
	public string? Interests { get; set; }

	/// <summary>
	/// Optional contact details the owner chooses to show publicly. These are
	/// deliberately separate from the private account email and phone number.
	/// </summary>
	public string? ContactEmail { get; set; }
	public string? ContactPhone { get; set; }
	public string? ContactAddress { get; set; }

	/// <summary>
	/// Public profile visibility. A hidden profile is still available to its
	/// owner and to administrators, but everyone else gets a 404.
	/// </summary>
	public bool IsProfilePublic { get; set; }

	public List<UserCertificate> Certificates { get; set; } = new();

	public List<UserQualification> Education { get; set; } = new();

	[NotMapped]
	public string FullName => $"{FirstName} {LastName}";

	public override string NormalizedEmail { get => base.Email!.ToUpper(); }
	public override string NormalizedUserName { get => base.UserName!.ToUpper(); }

	public string? PhotoUrl { get; set; }

	public override string UserName => Email!.Split('@')[0];

	public List<Post> Posts { get; set; } = new();

	public List<Notification> Notifications { get; set; } = new();

	public bool IsDeleted { get; set; }
}

