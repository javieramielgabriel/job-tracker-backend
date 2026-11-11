using System.ComponentModel.DataAnnotations;
using JobTracker.Api.Common.Constants;
using JobTracker.Api.Common.Pagination;

namespace JobTracker.Api.Models;

/// <summary>
/// Query parameters for filtering, searching, sorting, and paginating job applications.
/// </summary>
public sealed record JobApplicationQueryParameters : PaginationParameters
{
    public ApplicationStatus? Status { get; init; }

    [MaxLength(ApiConstants.Validation.MaxSearchLength)]
    public string? Search { get; init; }

    [AllowedValues("createdAt", "updatedAt", "appliedAt", "companyName", "jobTitle", "status",
        ErrorMessage = "SortBy must be one of: createdAt, updatedAt, appliedAt, companyName, jobTitle, status.")]
    public string SortBy { get; init; } = "createdAt";
}
