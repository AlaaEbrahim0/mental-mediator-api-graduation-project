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

	public static Error VisibilityUnsupported() => Error.Validation(
		"Posts.VisibilityUnsupported", "Posts for a specific group are not available yet.");
}
