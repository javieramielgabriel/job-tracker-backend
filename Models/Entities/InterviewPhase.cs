using JobTracker.Api.Models.Common;

namespace JobTracker.Api.Models;

/// <summary>
/// Database entity representing a single interview phase or stage within a job application.
/// </summary>
public sealed class InterviewPhase : BaseEntity
{
    public Guid JobApplicationId { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public string? Notes { get; set; }
    public JobApplication JobApplication { get; set; } = null!;
}
