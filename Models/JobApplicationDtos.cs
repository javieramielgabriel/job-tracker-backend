using System.ComponentModel.DataAnnotations;

namespace JobTracker.Api.Models;

public sealed record JobApplicationReadDto(
    Guid Id,
    string CompanyName,
    string JobTitle,
    string? Location,
    string? JobUrl,
    ApplicationStatus Status,
    DateTimeOffset? AppliedAtUtc,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<InterviewPhaseReadDto> InterviewPhases);

public sealed record InterviewPhaseReadDto(
    Guid Id,
    string Name,
    DateTimeOffset? ScheduledAtUtc,
    string? Notes);

public sealed record InterviewPhaseDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    public DateTimeOffset? ScheduledAtUtc { get; init; }

    [StringLength(2000)]
    public string? Notes { get; init; }
}

public sealed record JobApplicationCreateDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string CompanyName { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string JobTitle { get; init; }

    [StringLength(150)]
    public string? Location { get; init; }

    [Url, StringLength(2048)]
    public string? JobUrl { get; init; }

    [EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus Status { get; init; } = ApplicationStatus.Bookmarked;

    public DateTimeOffset? AppliedAtUtc { get; init; }

    [StringLength(4000)]
    public string? Notes { get; init; }

    public IReadOnlyList<InterviewPhaseDto> InterviewPhases { get; init; } = [];
}

public sealed record JobApplicationUpdateDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public required string CompanyName { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string JobTitle { get; init; }

    [StringLength(150)]
    public string? Location { get; init; }

    [Url, StringLength(2048)]
    public string? JobUrl { get; init; }

    [Required, EnumDataType(typeof(ApplicationStatus))]
    public required ApplicationStatus Status { get; init; }

    public DateTimeOffset? AppliedAtUtc { get; init; }

    [StringLength(4000)]
    public string? Notes { get; init; }

    public IReadOnlyList<InterviewPhaseDto> InterviewPhases { get; init; } = [];
}
