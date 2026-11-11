namespace JobTracker.Api.Services.Common;

/// <summary>
/// Generic service contract for standard read and delete CRUD operations by identifier.
/// </summary>
/// <typeparam name="TReadDto">The response DTO returned to callers.</typeparam>
public interface ICrudService<TReadDto>
{
    Task<IReadOnlyList<TReadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TReadDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
