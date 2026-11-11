using JobTracker.Api.Common.Constants;
using JobTracker.Api.Common.Pagination;
using JobTracker.Api.Models;
using JobTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

/// <summary>
/// MVC Controller responsible for handling HTTP requests for job applications.
/// Delegates all business logic, validation, and data access to <see cref="IJobApplicationService"/>.
/// </summary>
[Route(ApiConstants.Routes.JobApplications)]
[Tags("Job Applications")]
public sealed class JobApplicationsController(IJobApplicationService jobApplicationService) : BaseApiController
{
    /// <summary>
    /// Gets all job applications ordered by creation date descending.
    /// </summary>
    [HttpGet]
    [EndpointSummary("Gets all job applications.")]
    [ProducesResponseType(typeof(IReadOnlyList<JobApplicationReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobApplicationReadDto>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var applications = await jobApplicationService.GetAllAsync(cancellationToken);
        return Ok(applications);
    }

    /// <summary>
    /// Gets a paginated, filtered, and sorted page of job applications.
    /// </summary>
    [HttpGet("paged")]
    [EndpointSummary("Gets a paginated, filtered, and sorted list of job applications.")]
    [ProducesResponseType(typeof(PagedResponse<JobApplicationReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<JobApplicationReadDto>>> GetPagedAsync(
        [FromQuery] JobApplicationQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var pagedResult = await jobApplicationService.GetPagedAsync(queryParameters, cancellationToken);
        return Ok(pagedResult);
    }

    /// <summary>
    /// Gets a single job application by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}", Name = ApiConstants.RouteNames.GetJobApplicationById)]
    [EndpointSummary("Gets a job application by ID.")]
    [ProducesResponseType(typeof(JobApplicationReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobApplicationReadDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var application = await jobApplicationService.GetByIdAsync(id, cancellationToken);
        return Ok(application);
    }

    /// <summary>
    /// Creates a new job application along with any initial interview phases.
    /// </summary>
    [HttpPost]
    [EndpointSummary("Creates a job application.")]
    [ProducesResponseType(typeof(JobApplicationReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobApplicationReadDto>> CreateAsync(
        [FromBody] JobApplicationCreateDto request,
        CancellationToken cancellationToken)
    {
        var created = await jobApplicationService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute(
            routeName: ApiConstants.RouteNames.GetJobApplicationById,
            routeValues: new { id = created.Id },
            value: created);
    }

    /// <summary>
    /// Updates an existing job application and reconciles its interview phases in-place.
    /// </summary>
    [HttpPut("{id:guid}")]
    [EndpointSummary("Updates a job application and reconciles its interview phases.")]
    [ProducesResponseType(typeof(JobApplicationReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobApplicationReadDto>> UpdateAsync(
        Guid id,
        [FromBody] JobApplicationUpdateDto request,
        CancellationToken cancellationToken)
    {
        var updated = await jobApplicationService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Updates a job application's status and optionally its applied timestamp.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [EndpointSummary("Updates a job application's status and optionally its applied timestamp.")]
    [ProducesResponseType(typeof(JobApplicationReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobApplicationReadDto>> UpdateStatusAsync(
        Guid id,
        [FromBody] JobApplicationStatusUpdateDto request,
        CancellationToken cancellationToken)
    {
        var updated = await jobApplicationService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes a job application and cascades deletion to its interview phases.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [EndpointSummary("Deletes a job application.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await jobApplicationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
