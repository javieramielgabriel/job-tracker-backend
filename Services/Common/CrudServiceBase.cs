using AutoMapper;
using JobTracker.Api.Common.Exceptions;
using JobTracker.Api.Data;
using JobTracker.Api.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Services.Common;

/// <summary>
/// Reusable base service providing common read/delete CRUD operations, AutoMapper integration,
/// time abstraction, and optimistic concurrency handling.
/// </summary>
public abstract class CrudServiceBase<TEntity, TReadDto>(
    JobTrackerDbContext dbContext,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger logger) : ICrudService<TReadDto>
    where TEntity : BaseEntity
{
    protected JobTrackerDbContext DbContext { get; } = dbContext;
    protected IMapper Mapper { get; } = mapper;
    protected TimeProvider TimeProvider { get; } = timeProvider;
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Builds the base query used for read operations (override to include related collections or default ordering).
    /// </summary>
    protected virtual IQueryable<TEntity> BuildReadQuery() =>
        DbContext.Set<TEntity>().AsNoTracking();

    /// <summary>
    /// Creates the domain-specific not-found exception for a missing entity ID.
    /// </summary>
    protected abstract EntityNotFoundException CreateNotFoundException(Guid id);

    /// <summary>
    /// Creates the domain-specific concurrency conflict exception for an entity ID.
    /// </summary>
    protected abstract ConcurrencyConflictException CreateConcurrencyException(Guid id, DbUpdateConcurrencyException exception);

    public virtual async Task<IReadOnlyList<TReadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await BuildReadQuery().ToListAsync(cancellationToken);
        return Mapper.Map<List<TReadDto>>(entities);
    }

    public virtual async Task<TReadDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await BuildReadQuery()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (entity is null)
        {
            throw CreateNotFoundException(id);
        }

        return Mapper.Map<TReadDto>(entity);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await DbContext.Set<TEntity>()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (entity is null)
        {
            throw CreateNotFoundException(id);
        }

        DbContext.Set<TEntity>().Remove(entity);
        await SaveChangesWithConcurrencyCheckAsync(id, cancellationToken);
    }

    protected async Task SaveChangesWithConcurrencyCheckAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.LogWarning(ex, "Optimistic concurrency conflict detected for resource {ResourceId}", resourceId);
            throw CreateConcurrencyException(resourceId, ex);
        }
    }
}
