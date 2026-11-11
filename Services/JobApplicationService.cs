using AutoMapper;
using JobTracker.Api.Common.Exceptions;
using JobTracker.Api.Common.Pagination;
using JobTracker.Api.Common.Validation;
using JobTracker.Api.Data;
using JobTracker.Api.Models;
using JobTracker.Api.Services.Common;
using JobTracker.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Services;

/// <summary>
/// Encapsulates business logic, validation, mapping, and persistence operations for job applications.
/// </summary>
public sealed class JobApplicationService(
    JobTrackerDbContext dbContext,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<JobApplicationService> logger)
    : CrudServiceBase<JobApplication, JobApplicationReadDto>(dbContext, mapper, timeProvider, logger),
      IJobApplicationService
{
    protected override IQueryable<JobApplication> BuildReadQuery() =>
        DbContext.JobApplications
            .AsNoTracking()
            .Include(application => application.InterviewPhases)
            .OrderByDescending(application => application.CreatedAtUtc);

    protected override EntityNotFoundException CreateNotFoundException(Guid id) =>
        EntityNotFoundException.ForJobApplication(id);

    protected override ConcurrencyConflictException CreateConcurrencyException(
        Guid id,
        DbUpdateConcurrencyException exception) =>
        ConcurrencyConflictException.ForJobApplication(id, exception);

    public async Task<PagedResponse<JobApplicationReadDto>> GetPagedAsync(
        JobApplicationQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = RequestValidator.ValidateObject(queryParameters);
        if (validationErrors.Count > 0)
        {
            throw new DomainValidationException(validationErrors);
        }

        IQueryable<JobApplication> query = DbContext.JobApplications
            .AsNoTracking()
            .Include(application => application.InterviewPhases);

        if (queryParameters.Status.HasValue)
        {
            var status = queryParameters.Status.Value;
            query = query.Where(application => application.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
        {
            var searchTerm = queryParameters.Search.Trim().ToLower();
            query = query.Where(application =>
                application.CompanyName.ToLower().Contains(searchTerm) ||
                application.JobTitle.ToLower().Contains(searchTerm) ||
                (application.Location != null && application.Location.ToLower().Contains(searchTerm)));
        }

        query = ApplySorting(query, queryParameters.SortBy, queryParameters.SortDirection);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        var dtoItems = Mapper.Map<List<JobApplicationReadDto>>(items);

        return PagedResponse<JobApplicationReadDto>.Create(
            dtoItems,
            queryParameters.PageNumber,
            queryParameters.PageSize,
            totalCount);
    }

    public async Task<JobApplicationReadDto> CreateAsync(
        JobApplicationCreateDto request,
        CancellationToken cancellationToken = default)
    {
        var errors = RequestValidator.ValidateWithCollection(
            request,
            request.InterviewPhases,
            nameof(JobApplicationCreateDto.InterviewPhases));

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        var now = TimeProvider.GetUtcNow();
        var application = Mapper.Map<JobApplication>(request);

        application.CreatedAtUtc = now;
        application.UpdatedAtUtc = now;
        application.AppliedAtUtc = request.AppliedAtUtc
            ?? (ShouldAutoTimestampAppliedAt(request.Status) ? now : null);

        DbContext.JobApplications.Add(application);
        await DbContext.SaveChangesAsync(cancellationToken);

        Logger.LogInformation(
            "Created job application {ApplicationId} for {CompanyName} ({JobTitle})",
            application.Id,
            application.CompanyName,
            application.JobTitle);

        return Mapper.Map<JobApplicationReadDto>(application);
    }

    public async Task<JobApplicationReadDto> UpdateAsync(
        Guid id,
        JobApplicationUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var errors = RequestValidator.ValidateWithCollection(
            request,
            request.InterviewPhases,
            nameof(JobApplicationUpdateDto.InterviewPhases),
            childIdSelector: phase => phase.Id,
            validateUniqueIds: true,
            duplicateIdMessagePrefix: "Duplicate interview phase ID");

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        var application = await DbContext.JobApplications
            .Include(item => item.InterviewPhases)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (application is null)
        {
            throw CreateNotFoundException(id);
        }

        var existingPhasesById = application.InterviewPhases.ToDictionary(phase => phase.Id);
        var unknownPhaseErrors = new Dictionary<string, string[]>();

        for (var index = 0; index < request.InterviewPhases.Count; index++)
        {
            var phaseDto = request.InterviewPhases[index];
            if (phaseDto.Id is Guid phaseId && !existingPhasesById.ContainsKey(phaseId))
            {
                unknownPhaseErrors[$"InterviewPhases[{index}].Id"] =
                    [$"No interview phase with ID '{phaseId}' exists on job application '{id}'."];
            }
        }

        if (unknownPhaseErrors.Count > 0)
        {
            throw new DomainValidationException(unknownPhaseErrors);
        }

        Mapper.Map(request, application);
        application.UpdatedAtUtc = TimeProvider.GetUtcNow();

        ReconcileInterviewPhases(application, request.InterviewPhases, existingPhasesById);

        await SaveChangesWithConcurrencyCheckAsync(id, cancellationToken);

        Logger.LogInformation("Updated job application {ApplicationId}", id);

        return Mapper.Map<JobApplicationReadDto>(application);
    }

    public async Task<JobApplicationReadDto> UpdateStatusAsync(
        Guid id,
        JobApplicationStatusUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        var errors = RequestValidator.ValidateObject(request);
        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        var application = await DbContext.JobApplications
            .Include(item => item.InterviewPhases)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (application is null)
        {
            throw CreateNotFoundException(id);
        }

        var now = TimeProvider.GetUtcNow();
        application.Status = request.Status;

        if (request.AppliedAtUtc.HasValue)
        {
            application.AppliedAtUtc = request.AppliedAtUtc;
        }
        else if (application.AppliedAtUtc is null && ShouldAutoTimestampAppliedAt(request.Status))
        {
            application.AppliedAtUtc = now;
        }

        application.UpdatedAtUtc = now;

        await SaveChangesWithConcurrencyCheckAsync(id, cancellationToken);

        Logger.LogInformation(
            "Updated job application {ApplicationId} status to {Status}",
            id,
            request.Status);

        return Mapper.Map<JobApplicationReadDto>(application);
    }

    public static bool ShouldAutoTimestampAppliedAt(ApplicationStatus status) =>
        status is not (ApplicationStatus.Bookmarked or ApplicationStatus.Withdrawn);

    private void ReconcileInterviewPhases(
        JobApplication application,
        IReadOnlyList<InterviewPhaseDto> incomingPhases,
        IReadOnlyDictionary<Guid, InterviewPhase> existingPhasesById)
    {
        var incomingPhaseIds = incomingPhases
            .Where(phase => phase.Id.HasValue)
            .Select(phase => phase.Id!.Value)
            .ToHashSet();

        var phasesToRemove = application.InterviewPhases
            .Where(phase => !incomingPhaseIds.Contains(phase.Id))
            .ToList();

        foreach (var phaseToRemove in phasesToRemove)
        {
            application.InterviewPhases.Remove(phaseToRemove);
            DbContext.InterviewPhases.Remove(phaseToRemove);
        }

        foreach (var phaseDto in incomingPhases)
        {
            if (phaseDto.Id is Guid existingId && existingPhasesById.TryGetValue(existingId, out var existingPhase))
            {
                Mapper.Map(phaseDto, existingPhase);
            }
            else
            {
                var newPhase = Mapper.Map<InterviewPhase>(phaseDto);
                newPhase.JobApplicationId = application.Id;
                DbContext.InterviewPhases.Add(newPhase);
                if (!application.InterviewPhases.Contains(newPhase))
                {
                    application.InterviewPhases.Add(newPhase);
                }
            }
        }
    }

    private static IQueryable<JobApplication> ApplySorting(
        IQueryable<JobApplication> query,
        string sortBy,
        string sortDirection)
    {
        var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "companyname" => isAscending
                ? query.OrderBy(a => a.CompanyName).ThenByDescending(a => a.CreatedAtUtc)
                : query.OrderByDescending(a => a.CompanyName).ThenByDescending(a => a.CreatedAtUtc),
            "jobtitle" => isAscending
                ? query.OrderBy(a => a.JobTitle).ThenByDescending(a => a.CreatedAtUtc)
                : query.OrderByDescending(a => a.JobTitle).ThenByDescending(a => a.CreatedAtUtc),
            "status" => isAscending
                ? query.OrderBy(a => a.Status).ThenByDescending(a => a.CreatedAtUtc)
                : query.OrderByDescending(a => a.Status).ThenByDescending(a => a.CreatedAtUtc),
            "appliedat" => isAscending
                ? query.OrderBy(a => a.AppliedAtUtc).ThenByDescending(a => a.CreatedAtUtc)
                : query.OrderByDescending(a => a.AppliedAtUtc).ThenByDescending(a => a.CreatedAtUtc),
            "updatedat" => isAscending
                ? query.OrderBy(a => a.UpdatedAtUtc)
                : query.OrderByDescending(a => a.UpdatedAtUtc),
            _ => isAscending
                ? query.OrderBy(a => a.CreatedAtUtc)
                : query.OrderByDescending(a => a.CreatedAtUtc)
        };
    }
}
