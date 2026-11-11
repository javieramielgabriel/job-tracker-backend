namespace JobTracker.Api.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested entity does not exist (maps to HTTP 404 Not Found).
/// </summary>
public sealed class EntityNotFoundException(string title, string detail)
    : AppException(title, detail, StatusCodes.Status404NotFound)
{
    public static EntityNotFoundException ForJobApplication(Guid id) =>
        new(
            title: "Job application not found",
            detail: $"No job application exists with ID '{id}'.");

    public static EntityNotFoundException ForInterviewPhase(Guid applicationId, Guid phaseId) =>
        new(
            title: "Interview phase not found",
            detail: $"No interview phase with ID '{phaseId}' exists for job application '{applicationId}'.");
}
