using JobTracker.Api.Common.Pagination;
using JobTracker.Api.Models;
using JobTracker.Api.Services.Common;

namespace JobTracker.Api.Services.Interfaces;

/// <summary>
/// Application service contract for managing job applications and their lifecycle transitions.
/// </summary>
public interface IJobApplicationService : ICrudService<JobApplicationReadDto>
{
    Task<PagedResponse<JobApplicationReadDto>> GetPagedAsync(
        JobApplicationQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<JobApplicationReadDto> CreateAsync(
        JobApplicationCreateDto request,
        CancellationToken cancellationToken = default);

    Task<JobApplicationReadDto> UpdateAsync(
        Guid id,
        JobApplicationUpdateDto request,
        CancellationToken cancellationToken = default);

    Task<JobApplicationReadDto> UpdateStatusAsync(
        Guid id,
        JobApplicationStatusUpdateDto request,
        CancellationToken cancellationToken = default);
}
