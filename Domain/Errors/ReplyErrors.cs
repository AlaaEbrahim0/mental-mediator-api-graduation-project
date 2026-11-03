using Shared;

namespace Domain.Errors;

public static class ReplyErrors
{
    public static Error NotFound(int id) => Error.NotFound(
        "Replies.NotFound", $"reply with id = '{id}' was not found");

    public static Error Forbidden(int id) => Error.Forbidden(
        "Replies.Forbidden", $"you don't have a permission on reply with id = '{id}");

    public static Error InvalidMedia() => Error.Validation(
        "Replies.InvalidMedia",
        "Attach an image or video uploaded through the media endpoint first.");
}



