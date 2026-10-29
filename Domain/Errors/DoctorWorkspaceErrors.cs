using Shared;

namespace Domain.Errors;

public static class DoctorWorkspaceErrors
{
	public static Error NoteNotFound() => Error.NotFound(
		"Note.NotFound", "The patient note was not found.");

	public static Error NotAvailable() => Error.Forbidden(
		"Workspace.NotAvailable", "The current account is not available.");

	public static Error InvalidAppointment() => Error.Validation(
		"Workspace.InvalidAppointment", "The appointment does not belong to this patient and doctor.");

	public static Error FollowUpNotFound() => Error.NotFound(
		"FollowUp.NotFound", "The follow-up was not found.");

	public static Error PatientNotFound() => Error.Validation(
		"Workspace.PatientNotFound", "The selected patient was not found.");

	public static Error InvalidNoteTitle() => Error.Validation(
		"Workspace.InvalidNoteTitle", "A note title must be between 1 and 120 characters.");

	public static Error InvalidNoteBody() => Error.Validation(
		"Workspace.InvalidNoteBody", "A note body must be between 1 and 4000 characters.");

	public static Error InvalidFollowUpReason() => Error.Validation(
		"Workspace.InvalidFollowUpReason", "A follow-up reason must be between 1 and 200 characters.");

	public static Error InvalidFollowUpStatus() => Error.Validation(
		"Workspace.InvalidFollowUpStatus", "A follow-up status must be Open, Done or Cancelled.");

	public static Error InvalidFollowUpDueDate() => Error.Validation(
		"Workspace.InvalidFollowUpDueDate", "A follow-up due date cannot be in the past.");
}
