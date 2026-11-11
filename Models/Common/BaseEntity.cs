namespace JobTracker.Api.Models.Common;

/// <summary>
/// Base class for all database entities providing a unique identifier and optimistic concurrency token.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// PostgreSQL xmin system column used for optimistic concurrency control.
    /// </summary>
    public uint Version { get; set; }
}
