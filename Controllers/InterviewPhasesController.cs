using JobTracker.Api.Common.Constants;
using JobTracker.Api.Models;
using JobTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Controllers;

/// <summary>
/// MVC Controller responsible for handling HTTP requests for interview phases belonging to a job application.
/// Delegates all business logic, validation, and data access to <see cref="IInterviewPhaseService"/>.
/// </summary>
[Route(ApiConstants.Routes.InterviewPhases)]
[Tags("Interview Phases")]
public sealed class InterviewPhasesController(IInterviewPhaseService interviewPhaseService) : BaseApiController
{
    /// <summary>
    /// Gets all interview phases for a specific job application.
    /// </summary>
    [HttpGet]
    [EndpointSummary("Gets all interview phases for a job application.")]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewPhaseReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<InterviewPhaseReadDto>>> GetPhasesAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var phases = await interviewPhaseService.GetPhasesAsync(applicationId, cancellationToken);
        return Ok(phases);
    }

    /// <summary>
    /// Gets a single interview phase by its ID for a specific job application.
    /// </summary>
    [HttpGet("{phaseId:guid}", Name = ApiConstants.RouteNames.GetInterviewPhaseById)]
    [EndpointSummary("Gets a single interview phase by ID.")]
    [ProducesResponseType(typeof(InterviewPhaseReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InterviewPhaseReadDto>> GetPhaseByIdAsync(
        Guid applicationId,
        Guid phaseId,
        CancellationToken cancellationToken)
    {
        var phase = await interviewPhaseService.GetPhaseByIdAsync(applicationId, phaseId, cancellationToken);
        return Ok(phase);
    }

    /// <summary>
    /// Adds a new interview phase to a job application.
    /// </summary>
    [HttpPost]
    [EndpointSummary("Adds an interview phase to a job application.")]
    [ProducesResponseType(typeof(InterviewPhaseReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InterviewPhaseReadDto>> CreatePhaseAsync(
        Guid applicationId,
        [FromBody] InterviewPhaseDto request,
        CancellationToken cancellationToken)
    {
        var created = await interviewPhaseService.CreatePhaseAsync(applicationId, request, cancellationToken);
        return CreatedAtRoute(
            routeName: ApiConstants.RouteNames.GetInterviewPhaseById,
            routeValues: new { applicationId, phaseId = created.Id },
            value: created);
    }

    /// <summary>
    /// Updates an existing interview phase on a job application.
    /// </summary>
    [HttpPut("{phaseId:guid}")]
    [EndpointSummary("Updates an existing interview phase.")]
    [ProducesResponseType(typeof(InterviewPhaseReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InterviewPhaseReadDto>> UpdatePhaseAsync(
        Guid applicationId,
        Guid phaseId,
        [FromBody] InterviewPhaseDto request,
        CancellationToken cancellationToken)
    {
        var updated = await interviewPhaseService.UpdatePhaseAsync(applicationId, phaseId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes an interview phase from a job application.
    /// </summary>
    [HttpDelete("{phaseId:guid}")]
    [EndpointSummary("Deletes an interview phase from a job application.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeletePhaseAsync(
        Guid applicationId,
        Guid phaseId,
        CancellationToken cancellationToken)
    {
        await interviewPhaseService.DeletePhaseAsync(applicationId, phaseId, cancellationToken);
        return NoContent();
    }
}
