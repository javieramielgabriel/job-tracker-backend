using AutoMapper;
using JobTracker.Api.Common.Exceptions;
using JobTracker.Api.Common.Validation;
using JobTracker.Api.Data;
using JobTracker.Api.Models;
using JobTracker.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Services;

/// <summary>
/// Encapsulates business logic, validation, mapping, and persistence operations for interview phases.
/// </summary>
public sealed class InterviewPhaseService(
    JobTrackerDbContext dbContext,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<InterviewPhaseService> logger) : IInterviewPhaseService
{
    public async Task<IReadOnlyList<InterviewPhaseReadDto>> GetPhasesAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var applicationExists = await dbContext.JobApplications
            .AsNoTracking()
            .AnyAsync(application => application.Id == applicationId, cancellationToken);

        if (!applicationExists)
        {
            throw EntityNotFoundException.ForJobApplication(applicationId);
        }

        var phases = await dbContext.InterviewPhases
            .AsNoTracking()
            .Where(phase => phase.JobApplicationId == applicationId)
            .OrderBy(phase => phase.ScheduledAtUtc)
            .ThenBy(phase => phase.Id)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<InterviewPhaseReadDto>>(phases);
    }

    public async Task<InterviewPhaseReadDto> GetPhaseByIdAsync(
        Guid applicationId,
        Guid phaseId,
        CancellationToken cancellationToken = default)
    {
        var phase = await dbContext.InterviewPhases
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.JobApplicationId == applicationId && item.Id == phaseId,
                cancellationToken);

        if (phase is null)
        {
            var applicationExists = await dbContext.JobApplications
                .AsNoTracking()
                .AnyAsync(item => item.Id == applicationId, cancellationToken);

            throw applicationExists
                ? EntityNotFoundException.ForInterviewPhase(applicationId, phaseId)
                : EntityNotFoundException.ForJobApplication(applicationId);
        }

        return mapper.Map<InterviewPhaseReadDto>(phase);
    }

    public async Task<InterviewPhaseReadDto> CreatePhaseAsync(
        Guid applicationId,
        InterviewPhaseDto request,
        CancellationToken cancellationToken = default)
    {
        var errors = RequestValidator.ValidateObject(request);
        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        var application = await GetJobApplicationOrThrowAsync(applicationId, cancellationToken);

        var phase = mapper.Map<InterviewPhase>(request);
        phase.JobApplicationId = applicationId;

        dbContext.InterviewPhases.Add(phase);
        application.UpdatedAtUtc = timeProvider.GetUtcNow();

        await SaveChangesWithConcurrencyCheckAsync(applicationId, cancellationToken);

        logger.LogInformation(
            "Added interview phase {PhaseId} ('{PhaseName}') to job application {ApplicationId}",
            phase.Id,
            phase.Name,
            applicationId);

        return mapper.Map<InterviewPhaseReadDto>(phase);
    }

    public async Task<InterviewPhaseReadDto> UpdatePhaseAsync(
        Guid applicationId,
        Guid phaseId,
        InterviewPhaseDto request,
        CancellationToken cancellationToken = default)
    {
        var errors = RequestValidator.ValidateObject(request);
        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        var application = await GetJobApplicationOrThrowAsync(applicationId, cancellationToken);

        var phase = await dbContext.InterviewPhases
            .FirstOrDefaultAsync(
                item => item.JobApplicationId == applicationId && item.Id == phaseId,
                cancellationToken);

        if (phase is null)
        {
            throw EntityNotFoundException.ForInterviewPhase(applicationId, phaseId);
        }

        mapper.Map(request, phase);
        application.UpdatedAtUtc = timeProvider.GetUtcNow();

        await SaveChangesWithConcurrencyCheckAsync(applicationId, cancellationToken);

        logger.LogInformation(
            "Updated interview phase {PhaseId} on job application {ApplicationId}",
            phaseId,
            applicationId);

        return mapper.Map<InterviewPhaseReadDto>(phase);
    }

    public async Task DeletePhaseAsync(
        Guid applicationId,
        Guid phaseId,
        CancellationToken cancellationToken = default)
    {
        var application = await GetJobApplicationOrThrowAsync(applicationId, cancellationToken);

        var phase = await dbContext.InterviewPhases
            .FirstOrDefaultAsync(
                item => item.JobApplicationId == applicationId && item.Id == phaseId,
                cancellationToken);

        if (phase is null)
        {
            throw EntityNotFoundException.ForInterviewPhase(applicationId, phaseId);
        }

        dbContext.InterviewPhases.Remove(phase);
        application.UpdatedAtUtc = timeProvider.GetUtcNow();

        await SaveChangesWithConcurrencyCheckAsync(applicationId, cancellationToken);

        logger.LogInformation(
            "Deleted interview phase {PhaseId} from job application {ApplicationId}",
            phaseId,
            applicationId);
    }

    private async Task<JobApplication> GetJobApplicationOrThrowAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.JobApplications
            .FirstOrDefaultAsync(item => item.Id == applicationId, cancellationToken);

        if (application is null)
        {
            throw EntityNotFoundException.ForJobApplication(applicationId);
        }

        return application;
    }

    private async Task SaveChangesWithConcurrencyCheckAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(
                ex,
                "Optimistic concurrency conflict detected for job application {ApplicationId}",
                applicationId);
            throw ConcurrencyConflictException.ForJobApplication(applicationId, ex);
        }
    }
}
