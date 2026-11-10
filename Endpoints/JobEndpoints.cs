using System.ComponentModel.DataAnnotations;
using JobTracker.Api.Data;
using JobTracker.Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Endpoints;

public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/job-applications")
            .WithTags("Job Applications");

        group.MapGet("", GetAllAsync)
            .WithSummary("Gets all job applications.");
        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetJobApplicationById")
            .WithSummary("Gets a job application by ID.");
        group.MapPost("", CreateAsync)
            .WithSummary("Creates a job application.");
        group.MapPut("/{id:guid}", UpdateAsync)
            .WithSummary("Replaces a job application.");
        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithSummary("Deletes a job application.");

        return endpoints;
    }

    private static async Task<Ok<List<JobApplicationReadDto>>> GetAllAsync(
        JobTrackerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var applications = await dbContext.JobApplications
            .AsNoTracking()
            .Include(application => application.InterviewPhases)
            .OrderByDescending(application => application.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(applications.Select(ToReadDto).ToList());
    }

    private static async Task<Results<Ok<JobApplicationReadDto>, NotFound<ProblemDetails>>> GetByIdAsync(
        Guid id,
        JobTrackerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.JobApplications
            .AsNoTracking()
            .Include(item => item.InterviewPhases)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return application is null
            ? TypedResults.NotFound(CreateNotFoundProblem(id))
            : TypedResults.Ok(ToReadDto(application));
    }

    private static async Task<Results<CreatedAtRoute<JobApplicationReadDto>, ValidationProblem>> CreateAsync(
        JobApplicationCreateDto request,
        JobTrackerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = ValidateRequest(request, request.InterviewPhases);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var now = DateTimeOffset.UtcNow;
        var application = new JobApplication
        {
            CompanyName = request.CompanyName.Trim(),
            JobTitle = request.JobTitle.Trim(),
            Location = request.Location?.Trim(),
            JobUrl = request.JobUrl,
            Status = request.Status,
            AppliedAtUtc = request.AppliedAtUtc,
            Notes = request.Notes,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            InterviewPhases = request.InterviewPhases
                .Select(ToEntity)
                .ToList()
        };

        dbContext.JobApplications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.CreatedAtRoute(
            ToReadDto(application),
            routeName: "GetJobApplicationById",
            routeValues: new { id = application.Id });
    }

    private static async Task<Results<Ok<JobApplicationReadDto>, NotFound<ProblemDetails>, ValidationProblem>> UpdateAsync(
        Guid id,
        JobApplicationUpdateDto request,
        JobTrackerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = ValidateRequest(request, request.InterviewPhases);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var application = await dbContext.JobApplications
            .Include(item => item.InterviewPhases)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (application is null)
        {
            return TypedResults.NotFound(CreateNotFoundProblem(id));
        }

        application.CompanyName = request.CompanyName.Trim();
        application.JobTitle = request.JobTitle.Trim();
        application.Location = request.Location?.Trim();
        application.JobUrl = request.JobUrl;
        application.Status = request.Status;
        application.AppliedAtUtc = request.AppliedAtUtc;
        application.Notes = request.Notes;
        application.UpdatedAtUtc = DateTimeOffset.UtcNow;
        application.InterviewPhases.Clear();
        foreach (var phase in request.InterviewPhases)
        {
            application.InterviewPhases.Add(ToEntity(phase));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(ToReadDto(application));
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> DeleteAsync(
        Guid id,
        JobTrackerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.JobApplications
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (application is null)
        {
            return TypedResults.NotFound(CreateNotFoundProblem(id));
        }

        dbContext.JobApplications.Remove(application);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static Dictionary<string, string[]> ValidateRequest<TRequest>(
        TRequest request,
        IReadOnlyList<InterviewPhaseDto>? interviewPhases)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            request!,
            new ValidationContext(request!),
            validationResults,
            validateAllProperties: true);

        if (interviewPhases is null)
        {
            validationResults.Add(new ValidationResult(
                "InterviewPhases cannot be null.",
                [nameof(JobApplicationCreateDto.InterviewPhases)]));
        }
        else
        {
            for (var index = 0; index < interviewPhases.Count; index++)
            {
                var phase = interviewPhases[index];
                if (phase is null)
                {
                    validationResults.Add(new ValidationResult(
                        "Interview phase cannot be null.",
                        [$"InterviewPhases[{index}]"]));
                    continue;
                }

                var phaseResults = new List<ValidationResult>();
                Validator.TryValidateObject(
                    phase,
                    new ValidationContext(phase),
                    phaseResults,
                    validateAllProperties: true);
                validationResults.AddRange(phaseResults.Select(result =>
                    new ValidationResult(
                        result.ErrorMessage,
                        result.MemberNames.Select(name => $"InterviewPhases[{index}].{name}"))));
            }
        }

        return validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => new { Member = member, Error = result.ErrorMessage ?? "The value is invalid." }))
            .GroupBy(item => item.Member)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Error).Distinct().ToArray());
    }

    private static InterviewPhase ToEntity(InterviewPhaseDto phase) => new()
    {
        Name = phase.Name.Trim(),
        ScheduledAtUtc = phase.ScheduledAtUtc,
        Notes = phase.Notes
    };

    private static JobApplicationReadDto ToReadDto(JobApplication application) =>
        new(
            application.Id,
            application.CompanyName,
            application.JobTitle,
            application.Location,
            application.JobUrl,
            application.Status,
            application.AppliedAtUtc,
            application.Notes,
            application.CreatedAtUtc,
            application.UpdatedAtUtc,
            application.InterviewPhases
                .OrderBy(phase => phase.ScheduledAtUtc)
                .Select(phase => new InterviewPhaseReadDto(
                    phase.Id,
                    phase.Name,
                    phase.ScheduledAtUtc,
                    phase.Notes))
                .ToList());

    private static ProblemDetails CreateNotFoundProblem(Guid id) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Job application not found",
        Detail = $"No job application exists with ID '{id}'."
    };
}
