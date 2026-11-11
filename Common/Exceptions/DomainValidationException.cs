namespace JobTracker.Api.Common.Exceptions;

/// <summary>
/// Exception thrown when request or business validation fails (maps to HTTP 400 Bad Request with ValidationProblemDetails).
/// </summary>
public sealed class DomainValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public DomainValidationException(
        IReadOnlyDictionary<string, string[]> errors,
        string title = "One or more validation errors occurred.",
        string detail = "See the errors property for details.")
        : base(title, detail, StatusCodes.Status400BadRequest)
    {
        Errors = errors;
    }
}
