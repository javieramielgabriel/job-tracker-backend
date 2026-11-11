using JobTracker.Api.Models.Common;

namespace JobTracker.Api.Models;

/// <summary>
/// Database entity representing a job application and its associated interview phases.
/// </summary>
public sealed class JobApplication : AuditableEntity
{
    public required string CompanyName { get; set; }
    public required string JobTitle { get; set; }
    public string? Location { get; set; }
    public string? JobUrl { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Bookmarked;
    public DateTimeOffset? AppliedAtUtc { get; set; }
    public string? Notes { get; set; }
    public ICollection<InterviewPhase> InterviewPhases { get; set; } = new List<InterviewPhase>();
}
