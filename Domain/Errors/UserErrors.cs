using Shared;

namespace Domain.Errors;
public static class UserErrors
{
    public static Error NotFound(string id) => Error.NotFound(
        "Users.NotFound", $"User with id = '{id}' was not found");

    public static Error NotFoundByEmail(string email) => Error.NotFound(
        "Users.NotFoundByEmail", $"User with email = '{email}' was not found");

    public static Error EmailNotUnique(string email) => Error.Conflict(
        "Users.EmailNotUnique", $"The provided email: '{email} is not unique");

    public static Error InvalidCredentials() => Error.Validation(
        "Users.InvalidCredentials", "The provided email or password is invalid");

    public static Error EmailNotConfirmed() => Error.Failure(
        "Users.EmailNotConfirmed", "Email has not been confirmed yet");

    public static Error InvalidToken(string description) => Error.Validation(
        "Users.InvalidToken", description);

    public static Error ValidationErrors(string description) => Error.Validation(
        "Users.ValidationErrors", description);

    public static Error EmailAlreadyConfirmed(string email) => Error.Conflict(
        "Users.EmailAlreadyConfirmed", $"email [{email}] has been already confirmed");
}

