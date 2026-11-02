using Shared;

namespace Domain.Errors;

public static class DoctorRatingErrors
{
	public static Error DoctorNotFound(string doctorId) => Error.NotFound(
		"DoctorRatings.DoctorNotFound", $"No doctor was found for id '{doctorId}'.");

	public static Error PatientsOnly() => Error.Forbidden(
		"DoctorRatings.PatientsOnly", "Only patient accounts can rate doctors.");

	public static Error SelfRating() => Error.Forbidden(
		"DoctorRatings.SelfRating", "A doctor cannot rate their own profile.");

	public static Error NotRated() => Error.NotFound(
		"DoctorRatings.NotRated", "You have not rated this doctor yet.");
}
