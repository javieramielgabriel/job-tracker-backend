namespace JobTracker.Api.Common.Pagination;

/// <summary>
/// Generic envelope for paginated API responses with metadata for UI pagination controls.
/// </summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static PagedResponse<T> Create(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        var safePageSize = Math.Max(1, pageSize);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);

        return new PagedResponse<T>(
            Items: items,
            PageNumber: pageNumber,
            PageSize: safePageSize,
            TotalCount: totalCount,
            TotalPages: totalPages,
            HasPreviousPage: pageNumber > 1,
            HasNextPage: pageNumber < totalPages);
    }
}
