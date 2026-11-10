namespace JobTracker.Api.Models;

public sealed class InterviewPhase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobApplicationId { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public string? Notes { get; set; }
    public JobApplication JobApplication { get; set; } = null!;
}
