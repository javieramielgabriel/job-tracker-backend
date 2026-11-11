namespace JobTracker.Api.Common.Exceptions;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict occurs during a database update or delete (maps to HTTP 409 Conflict).
/// </summary>
public sealed class ConcurrencyConflictException(string title, string detail, Exception? innerException = null)
    : AppException(title, detail, StatusCodes.Status409Conflict, innerException)
{
    public static ConcurrencyConflictException ForJobApplication(Guid id, Exception? innerException = null) =>
        new(
            title: "Concurrency conflict",
            detail: $"Job application '{id}' or its interview phases were modified by another request. Please refresh and try again.",
            innerException: innerException);
}
