namespace JobTracker.Api.Models;

public sealed class JobApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string CompanyName { get; set; }
    public required string JobTitle { get; set; }
    public string? Location { get; set; }
    public string? JobUrl { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Bookmarked;
    public DateTimeOffset? AppliedAtUtc { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<InterviewPhase> InterviewPhases { get; set; } = new List<InterviewPhase>();
}
