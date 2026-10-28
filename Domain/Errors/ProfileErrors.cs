using Shared;

namespace Domain.Errors;

public static class ProfileErrors
{
	public static Error CertificateNotFound(int id) => Error.NotFound(
		"Profiles.CertificateNotFound",
		$"Certificate with id = '{id}' was not found on this profile.");

	public static Error QualificationNotFound(int id) => Error.NotFound(
		"Profiles.QualificationNotFound",
		$"Qualification with id = '{id}' was not found on this profile.");

	public static Error DoctorDetailsNotAllowed() => Error.Validation(
		"Profiles.DoctorDetailsNotAllowed",
		"Doctor practice details can only be updated on a doctor profile.");

	public static Error HiddenProfile() => Error.NotFound(
		"Profiles.NotFound",
		"The requested profile was not found.");
}
