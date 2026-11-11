using System.ComponentModel.DataAnnotations;
using JobTracker.Api.Common.Constants;

namespace JobTracker.Api.Common.Pagination;

/// <summary>
/// Base query parameters for paginated and sorted API requests.
/// </summary>
public record PaginationParameters
{
    private int _pageNumber = ApiConstants.Pagination.DefaultPageNumber;
    private int _pageSize = ApiConstants.Pagination.DefaultPageSize;

    [Range(1, int.MaxValue)]
    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? ApiConstants.Pagination.DefaultPageNumber : value;
    }

    [Range(1, ApiConstants.Pagination.MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, ApiConstants.Pagination.MaxPageSize);
    }

    [AllowedValues("asc", "desc", ErrorMessage = "SortDirection must be 'asc' or 'desc'.")]
    public string SortDirection { get; init; } = "desc";
}
