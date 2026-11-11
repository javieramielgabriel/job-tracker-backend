using System.ComponentModel.DataAnnotations;
using JobTracker.Api.Common.Constants;

namespace JobTracker.Api.Models;

/// <summary>
/// Response DTO representing an interview phase returned by the API.
/// </summary>
public sealed record InterviewPhaseReadDto(
    Guid Id,
    string Name,
    DateTimeOffset? ScheduledAtUtc,
    string? Notes);

/// <summary>
/// Request DTO for creating or updating an interview phase.
/// </summary>
public sealed record InterviewPhaseDto
{
    public Guid? Id { get; init; }

    [Required, StringLength(ApiConstants.Validation.MaxPhaseNameLength, MinimumLength = 1)]
    public required string Name { get; init; }

    public DateTimeOffset? ScheduledAtUtc { get; init; }

    [StringLength(ApiConstants.Validation.MaxPhaseNotesLength)]
    public string? Notes { get; init; }
}
