namespace Tempest.Application.Common.Results;

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
}

/// <summary>Expected failure of a use case (never used for programming errors).</summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    private static readonly IReadOnlyDictionary<string, string[]> NoFieldErrors = new Dictionary<string, string[]>();

    public static readonly Error Unexpected = new("unexpected", "Ocurrió un error inesperado. Inténtalo nuevamente.");

    /// <summary>Messages keyed by request property name, so the UI can highlight the matching input.</summary>
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = NoFieldErrors;

    public static Error Failure(string code, string message) => new(code, message);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Validation(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new("validation", "Revisa los campos marcados.", ErrorType.Validation) { FieldErrors = fieldErrors };

    public static Error Validation(string field, string message) =>
        new("validation", message, ErrorType.Validation)
        {
            FieldErrors = new Dictionary<string, string[]> { [field] = [message] },
        };
}
