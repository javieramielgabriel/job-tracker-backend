using System.ComponentModel.DataAnnotations;
using JobTracker.Api.Common.Constants;

namespace JobTracker.Api.Models;

/// <summary>
/// Response DTO representing a job application and its ordered interview phases.
/// </summary>
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

/// <summary>
/// Request DTO for updating a job application's status and optional applied timestamp.
/// </summary>
public sealed record JobApplicationStatusUpdateDto
{
    [Required, EnumDataType(typeof(ApplicationStatus))]
    public required ApplicationStatus Status { get; init; }

    public DateTimeOffset? AppliedAtUtc { get; init; }
}

/// <summary>
/// Request DTO for creating a new job application.
/// </summary>
public sealed record JobApplicationCreateDto
{
    [Required, StringLength(ApiConstants.Validation.MaxCompanyNameLength, MinimumLength = 1)]
    public required string CompanyName { get; init; }

    [Required, StringLength(ApiConstants.Validation.MaxJobTitleLength, MinimumLength = 1)]
    public required string JobTitle { get; init; }

    [StringLength(ApiConstants.Validation.MaxLocationLength)]
    public string? Location { get; init; }

    [Url, StringLength(ApiConstants.Validation.MaxJobUrlLength)]
    public string? JobUrl { get; init; }

    [EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus Status { get; init; } = ApplicationStatus.Bookmarked;

    public DateTimeOffset? AppliedAtUtc { get; init; }

    [StringLength(ApiConstants.Validation.MaxApplicationNotesLength)]
    public string? Notes { get; init; }

    public IReadOnlyList<InterviewPhaseDto> InterviewPhases { get; init; } = [];
}

/// <summary>
/// Request DTO for updating an existing job application and reconciling its interview phases.
/// </summary>
public sealed record JobApplicationUpdateDto
{
    [Required, StringLength(ApiConstants.Validation.MaxCompanyNameLength, MinimumLength = 1)]
    public required string CompanyName { get; init; }

    [Required, StringLength(ApiConstants.Validation.MaxJobTitleLength, MinimumLength = 1)]
    public required string JobTitle { get; init; }

    [StringLength(ApiConstants.Validation.MaxLocationLength)]
    public string? Location { get; init; }

    [Url, StringLength(ApiConstants.Validation.MaxJobUrlLength)]
    public string? JobUrl { get; init; }

    [Required, EnumDataType(typeof(ApplicationStatus))]
    public required ApplicationStatus Status { get; init; }

    public DateTimeOffset? AppliedAtUtc { get; init; }

    [StringLength(ApiConstants.Validation.MaxApplicationNotesLength)]
    public string? Notes { get; init; }

    public IReadOnlyList<InterviewPhaseDto> InterviewPhases { get; init; } = [];
}
