namespace Shared;

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new("Error.NullValue", "Error value was provided", ErrorType.Failure);

    private Error(string code, string description, ErrorType errorType)
    {
        Code = code;
        Description = description;
        ErrorType = errorType;
    }

    public string Code { get; set; }

    public string Description { get; set; }

    public ErrorType ErrorType { get; }
    public ErrorType Type { get; set; }

    public static Error NotFound(string code, string description) =>
        new Error(code, description, ErrorType.NotFound);

    public static Error Validation(string code, string description) =>
        new Error(code, description, ErrorType.Validation);

    public static Error Failure(string code, string description) =>
        new Error(code, description, ErrorType.Failure);

    public static Error Conflict(string code, string description) =>
        new Error(code, description, ErrorType.Conflict);

    public static Error ServiceUnavailable(string code, string description) =>
        new Error(code, description, ErrorType.ServiceUnavailable);

    public static Error Forbidden(string code, string description) =>
        new Error(code, description, ErrorType.Forbidden);

}
