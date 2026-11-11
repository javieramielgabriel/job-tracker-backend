using JobTracker.Api.Models;

namespace JobTracker.Api.Services.Interfaces;

/// <summary>
/// Application service contract for managing interview phases belonging to a job application.
/// </summary>
public interface IInterviewPhaseService
{
    Task<IReadOnlyList<InterviewPhaseReadDto>> GetPhasesAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<InterviewPhaseReadDto> GetPhaseByIdAsync(
        Guid applicationId,
        Guid phaseId,
        CancellationToken cancellationToken = default);

    Task<InterviewPhaseReadDto> CreatePhaseAsync(
        Guid applicationId,
        InterviewPhaseDto request,
        CancellationToken cancellationToken = default);

    Task<InterviewPhaseReadDto> UpdatePhaseAsync(
        Guid applicationId,
        Guid phaseId,
        InterviewPhaseDto request,
        CancellationToken cancellationToken = default);

    Task DeletePhaseAsync(
        Guid applicationId,
        Guid phaseId,
        CancellationToken cancellationToken = default);
}
