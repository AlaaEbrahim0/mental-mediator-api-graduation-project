using Shared;

namespace Domain.Errors;

public static class CommentErrors
{
    public static Error NotFound(int id) => Error.NotFound(
        "Comments.NotFound", $"Comment with id = '{id}' was not found");

    public static Error Forbidden(int id) => Error.Forbidden(
        "Comments.Forbidden", $"you don't have a permission on post with id = '{id}");
}

