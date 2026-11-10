---
name: dotnet-api-development
description: >-
  Use this skill when building, updating, or reviewing ASP.NET Core REST APIs
  using Controllers or Minimal APIs. Covers routing, HTTP methods/status codes,
  request validation, ProblemDetails, global exception handling, JWT auth,
  API versioning, Swagger/OpenAPI, CORS, pagination, filtering, sorting, search,
  structured logging, and rate limiting.
---

# ASP.NET Core API Development (`dotnet-api-development`)

## Overview
Provides comprehensive standards for designing, implementing, documenting, and securing maintainable RESTful APIs in **ASP.NET Core (.NET 10)** for the **Job Tracker Backend**, ensuring consistent contracts with the Angular frontend.

## Activation Instructions
Activate this skill whenever you are:
- Creating or modifying API Controllers (`[ApiController]`) or Minimal API route groups (`MapGroup`).
- Configuring routing, HTTP status codes, `ProblemDetails`, CORS, rate limiting, API versioning, or OpenAPI/Swagger documentation.
- Implementing query capabilities on list endpoints (pagination, filtering, sorting, search).
- Securing endpoints with JWT Bearer authentication and role/policy-based authorization.

## Specific Responsibilities
1. **Enforce RESTful Resource & Routing Conventions**: Use plural kebab-case resource nouns (`/api/v1/job-applications`), standard HTTP methods (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`), and accurate status codes (`200`, `201`, `204`, `400`, `401`, `403`, `404`, `409`, `422`, `429`).
2. **Guarantee Consistent Response & Error Contracts**: Use `ActionResult<T>` or `TypedResults`, `[ProducesResponseType]`, and RFC 9457 `ProblemDetails` / `ValidationProblemDetails` for all errors.
3. **Implement Safe Querying (Pagination, Filtering, Sorting, Search)**: Bound page sizes, whitelist sortable columns, and sanitize search terms.
4. **Configure Production-Ready Cross-Cutting Concerns**: Set up explicit CORS policies for the Angular frontend, JWT authentication, rate limiting, and OpenAPI schema generation.

---

## Technology-Specific Implementation Guidance

### 1. RESTful Controller Implementation
```csharp
using Asp.Versioning;
using JobTracker.Application.Contracts.Applications;
using JobTracker.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobTracker.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/job-applications")]
[Authorize]
[EnableRateLimiting("api-standard")]
[Produces("application/json")]
public sealed class JobApplicationsController(
    IJobApplicationService applicationService,
    ILogger<JobApplicationsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<JobApplicationSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<JobApplicationSummaryDto>>> GetListAsync(
        [FromQuery] JobApplicationQueryParameters query,
        CancellationToken ct)
    {
        var result = await applicationService.GetPagedAsync(query, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = "GetJobApplicationById")]
    [ProducesResponseType(typeof(JobApplicationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobApplicationDetailDto>> GetByIdAsync(
        Guid id,
        CancellationToken ct)
    {
        var application = await applicationService.GetByIdAsync(id, ct);
        if (application is null)
        {
            return Problem(
                title: "Job Application Not Found",
                detail: $"No job application exists with ID '{id}'.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(application);
    }

    [HttpPost]
    [ProducesResponseType(typeof(JobApplicationDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobApplicationDetailDto>> CreateAsync(
        [FromBody] CreateJobApplicationRequest request,
        CancellationToken ct)
    {
        var created = await applicationService.CreateAsync(request, ct);
        logger.LogInformation("Created job application {ApplicationId} for company {CompanyId}",
            created.Id, request.CompanyId);

        return CreatedAtRoute(
            routeName: "GetJobApplicationById",
            routeValues: new { version = "1.0", id = created.Id },
            value: created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(JobApplicationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobApplicationDetailDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateJobApplicationRequest request,
        CancellationToken ct)
    {
        var updated = await applicationService.UpdateAsync(id, request, ct);
        return updated is null
            ? Problem("Job Application Not Found", $"No job application exists with ID '{id}'.", StatusCodes.Status404NotFound)
            : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var deleted = await applicationService.DeleteAsync(id, ct);
        return deleted
            ? NoContent()
            : Problem("Job Application Not Found", $"No job application exists with ID '{id}'.", StatusCodes.Status404NotFound);
    }
}
```

### 2. Minimal APIs Alternative (TypedResults & Route Groups)
When using Minimal APIs, always return `Results<T1, T2>` with `TypedResults` so OpenAPI metadata is inferred automatically:
```csharp
public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/companies")
            .WithTags("Companies")
            .RequireAuthorization()
            .RequireRateLimiting("api-standard");

        group.MapGet("/{id:guid}", GetCompanyByIdAsync)
            .WithName("GetCompanyById")
            .WithSummary("Gets a company by its unique identifier.");

        return app;
    }

    private static async Task<Results<Ok<CompanyBriefDto>, NotFound<ProblemDetails>>> GetCompanyByIdAsync(
        Guid id,
        ICompanyService service,
        CancellationToken ct)
    {
        var company = await service.GetByIdAsync(id, ct);
        return company is not null
            ? TypedResults.Ok(company)
            : TypedResults.NotFound(new ProblemDetails
            {
                Title = "Company Not Found",
                Detail = $"Company '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
    }
}
```

### 3. Pagination, Filtering, Sorting, and Search
Define a strongly typed query parameter model with safe defaults and bounds:
```csharp
using System.ComponentModel.DataAnnotations;
using JobTracker.Domain.Entities;

namespace JobTracker.Application.Contracts.Applications;

public sealed record JobApplicationQueryParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    public ApplicationStatus? Status { get; init; }
    public WorkMode? WorkMode { get; init; }
    public Guid? CompanyId { get; init; }

    [MaxLength(100)]
    public string? Search { get; init; }

    [AllowedValues("appliedAt", "jobTitle", "companyName", "salaryMax", "status",
        ErrorMessage = "Invalid sortBy field.")]
    public string SortBy { get; init; } = "appliedAt";

    [AllowedValues("asc", "desc", ErrorMessage = "SortDirection must be 'asc' or 'desc'.")]
    public string SortDirection { get; init; } = "desc";
}
```

See [REST API and Security Patterns Reference](./references/rest-api-and-security-patterns.md) for full configuration of JWT Bearer Authentication, CORS for Angular, Rate Limiting, API Versioning, and Swagger/OpenAPI.

---

## CLI Commands
```powershell
# Run the API with launch profile
dotnet run --project src/JobTracker.Api --launch-profile https

# Test API endpoints via curl or .http files
curl -i https://localhost:7100/api/v1/job-applications?pageNumber=1&pageSize=10
```

---

## Common Mistakes to Avoid
1. **Returning `200 OK` on Resource Creation or Errors**: Return `201 Created` with a `Location` header for `POST` creation, and `4xx`/`5xx` `ProblemDetails` for errors.
2. **Unbounded List Endpoints**: Returning `dbContext.JobApplications.ToListAsync()` without pagination can exhaust memory and crash the API.
3. **Permissive CORS (`AllowAnyOrigin()` + `AllowCredentials()`)**: Always bind explicit allowed origins from configuration (e.g., `http://localhost:4200` for Angular dev).
4. **Dynamic String Sorting Without Whitelisting**: Accepting arbitrary column names for sorting without an explicit switch mapping risks runtime exceptions or injection.

---

## Verification Checklist
- [ ] All endpoints use plural resource nouns, proper HTTP verbs, and explicit `[ProducesResponseType]` or `TypedResults`.
- [ ] `POST` creation endpoints return `201 Created` with a valid `Location` header (`CreatedAtRoute` or `CreatedAtAction`).
- [ ] All validation and domain errors return RFC 9457 `ProblemDetails` or `ValidationProblemDetails`.
- [ ] List endpoints support bounded pagination (`PageSize <= 100`), whitelisted sorting, and safe filtering.
- [ ] CORS, JWT Authentication, Authorization policies, and Rate Limiting are registered in the correct middleware order.

---

## Official Documentation References
- [ASP.NET Core Web API Documentation](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Minimal APIs Overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview)
- [Handle Errors in ASP.NET Core APIs (ProblemDetails)](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors)
- [Rate Limiting Middleware in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [OpenAPI Support in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview)
