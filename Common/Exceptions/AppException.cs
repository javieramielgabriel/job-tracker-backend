namespace JobTracker.Api.Common.Exceptions;

/// <summary>
/// Base exception for known application and domain errors that map to HTTP ProblemDetails responses.
/// </summary>
public abstract class AppException(string title, string detail, int statusCode, Exception? innerException = null)
    : Exception(detail, innerException)
{
    public string Title { get; } = title;
    public int StatusCode { get; } = statusCode;
}
