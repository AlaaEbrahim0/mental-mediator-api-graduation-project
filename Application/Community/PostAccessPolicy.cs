using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.Community;

/// <summary>
/// Single read policy for posts and for every endpoint that exposes post
/// content. Comments and replies must use this too, otherwise a restricted post
/// leaks through a secondary URL.
/// </summary>
public static class PostAccessPolicy
{
	public static bool IsDoctorOrAdmin(string? role) =>
		string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase) ||
		string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

	public static bool IsModerator(string? role) =>
		string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
		string.Equals(role, "Moderator", StringComparison.OrdinalIgnoreCase);

	public static async Task<bool> CanReadAsync(
		Post post,
		string? userId,
		string? role,
		IChatRoomRepository rooms)
	{
		var isOwner = !string.IsNullOrEmpty(userId) &&
			string.Equals(post.AppUserId, userId, StringComparison.Ordinal);

		if (post.IsDraft)
		{
			return isOwner || IsModerator(role);
		}

		switch (post.Visibility)
		{
			case PostVisibility.Public:
				return true;
			case PostVisibility.DoctorsOnly:
				return IsDoctorOrAdmin(role);
			case PostVisibility.SpecificGroup:
				if (post.TargetChatRoomId is null)
				{
					return false;
				}
				if (isOwner || IsModerator(role))
				{
					return true;
				}
				if (string.IsNullOrEmpty(userId))
				{
					return false;
				}
				var membership = await rooms.GetMembershipAsync(post.TargetChatRoomId.Value, userId, false);
				return membership is not null && membership.LeftAtUtc is null;
			default:
				return false;
		}
	}

	/// <summary>
	/// Discussion on an anonymous post is limited to the author and clinical
	/// staff; everybody else may read but not add comments or replies.
	/// </summary>
	public static bool CanDiscuss(Post post, string? userId, string? role) =>
		!post.IsAnonymous ||
		IsDoctorOrAdmin(role) ||
		string.Equals(post.AppUserId, userId, StringComparison.Ordinal);
}
