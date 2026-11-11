namespace JobTracker.Api.Models.Common;

/// <summary>
/// Base class for entities that track creation and last modification UTC timestamps.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
