using Shared;

namespace Domain.Errors;


public static class PostErrors
{
	public static Error NotFound(int id) => Error.NotFound(
		"Posts.NotFound", $"Post with id = '{id}' was not found");

	public static Error Forbidden(int id) => Error.Forbidden(
		"Posts.Forbidden", $"you have no access to post with id = '{id}");

	public static Error DoctorsOnlyForbidden() => Error.Forbidden(
		"Posts.DoctorsOnlyForbidden", "Only doctors can publish to the Doctors Only audience.");

	public static Error SpecificGroupRequired() => Error.Validation(
		"Posts.SpecificGroupRequired", "Choose a group for the Specific Group audience.");

	public static Error SpecificGroupForbidden() => Error.Forbidden(
		"Posts.SpecificGroupForbidden", "Only active members of a group can post to it.");

	public static Error SpecificGroupNotFound() => Error.Validation(
		"Posts.SpecificGroupNotFound", "That group is not available.");

	public static Error MediaAttached() => Error.Conflict(
		"Posts.MediaAttached", "This media is already attached to a post and cannot be removed.");
}
