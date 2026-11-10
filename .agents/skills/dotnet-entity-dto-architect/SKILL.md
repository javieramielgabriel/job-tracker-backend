---
name: dotnet-entity-dto-architect
description: >-
  Use this skill when designing, generating, or refactoring domain entities,
  EF Core database entities, Request/Response/Update DTOs, entity relationships
  (1:1, 1:N, M:N), navigation properties, Fluent API configurations, mapping
  profiles (manual or AutoMapper), and validation rules.
---

# .NET Entity & DTO Architect (`dotnet-entity-dto-architect`)

## Overview
Specializes in designing clean, decoupled domain entities, persistence models, and API Data Transfer Objects (DTOs) for the **Job Tracker Backend**, ensuring strict contract boundaries between database schemas and external API consumers.

## Activation Instructions
Activate this skill whenever you are:
- Creating or modifying domain models, database entities, value objects, or enums.
- Designing API request, update, filter, or response DTOs (including nested DTOs and paginated envelopes).
- Defining 1:1, 1:N, or M:N relationships, navigation properties, and Fluent API configurations.
- Implementing object mapping (manual extension methods or AutoMapper profiles) and validation rules.

## Specific Responsibilities
1. **Enforce Strict Contract Separation**: Never expose EF Core database entities directly in API responses or accept them in API request bodies.
2. **Model Relationships Explicitly**: Configure foreign keys, navigation properties, delete behaviors, and join entities explicitly via `IEntityTypeConfiguration<T>`.
3. **Design Purpose-Built DTOs**: Create distinct `Create*Request`, `Update*Request`, `*SummaryDto`, and `*DetailDto` records with clear nullable annotations and validation rules.
4. **Choose Optimal Mapping Strategies**: Prefer compile-time safe manual mapping extension methods for simplicity and performance; use AutoMapper `Profile` classes with `ProjectTo<T>` only when existing project architecture standardizes on AutoMapper.

---

## Technology-Specific Implementation Guidance

### 1. Domain & Database Entities vs. API Contracts
- **Entities** encapsulate identity (`Guid Id`), audit metadata (`CreatedAtUtc`, `UpdatedAtUtc`), navigation properties, and domain invariants.
- **Request DTOs** (`CreateJobApplicationRequest`, `UpdateJobApplicationRequest`) represent client input, validated via DataAnnotations or FluentValidation.
- **Response DTOs** (`JobApplicationDetailDto`, `JobApplicationSummaryDto`) shape data specifically for UI views without circular navigation references.

### 2. Entity Relationships (1:1, 1:N, M:N)
In the Job Tracker domain:
- **One-to-One (1:1)**: `JobApplication` <-> `OfferDetails` (an application has at most one formal job offer).
- **One-to-Many (1:N)**: `Company` -> `JobApplication` (one company has many job applications); `JobApplication` -> `InterviewRound` (one application has multiple interview stages).
- **Many-to-Many (M:N)**: `JobApplication` <-> `SkillTag` (an application requires multiple skills, and a skill applies to many applications, optionally with an explicit join entity `JobApplicationSkill`).

```csharp
namespace JobTracker.Domain.Entities;

public enum ApplicationStatus
{
    Draft = 0,
    Applied = 1,
    Interviewing = 2,
    Offered = 3,
    Accepted = 4,
    Rejected = 5,
    Withdrawn = 6
}

public enum WorkMode
{
    OnSite = 0,
    Hybrid = 1,
    Remote = 2
}

public sealed class Company
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Industry { get; set; }

    // 1:N Navigation
    public ICollection<JobApplication> JobApplications { get; init; } = new List<JobApplication>();
}

public sealed class JobApplication
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string JobTitle { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public WorkMode WorkMode { get; set; } = WorkMode.Hybrid;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? JobPostingUrl { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset AppliedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    // 1:N Foreign Key & Navigation to Company
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // 1:1 Optional Navigation to OfferDetails
    public OfferDetails? Offer { get; set; }

    // 1:N Navigation to InterviewRounds
    public ICollection<InterviewRound> Interviews { get; init; } = new List<InterviewRound>();

    // M:N Navigation to SkillTag
    public ICollection<SkillTag> SkillTags { get; init; } = new List<SkillTag>();
}

public sealed class OfferDetails
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid JobApplicationId { get; set; }
    public required decimal BaseSalary { get; set; }
    public decimal? BonusAmount { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public JobApplication JobApplication { get; set; } = null!;
}

public sealed class InterviewRound
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid JobApplicationId { get; set; }
    public int RoundNumber { get; set; }
    public required string StageName { get; set; }
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public string? InterviewerName { get; set; }
    public JobApplication JobApplication { get; set; } = null!;
}

public sealed class SkillTag
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public ICollection<JobApplication> JobApplications { get; init; } = new List<JobApplication>();
}
```

### 3. Request, Update, Nested Response & Paginated DTOs
```csharp
using System.ComponentModel.DataAnnotations;

namespace JobTracker.Application.Contracts.Applications;

public sealed record CreateJobApplicationRequest : IValidatableObject
{
    [Required, StringLength(150, MinimumLength = 2)]
    public required string JobTitle { get; init; }

    [Required]
    public required Guid CompanyId { get; init; }

    public ApplicationStatus Status { get; init; } = ApplicationStatus.Applied;
    public WorkMode WorkMode { get; init; } = WorkMode.Hybrid;

    [Range(0, 10_000_000)]
    public decimal? SalaryMin { get; init; }

    [Range(0, 10_000_000)]
    public decimal? SalaryMax { get; init; }

    [Url, MaxLength(500)]
    public string? JobPostingUrl { get; init; }

    [MaxLength(4000)]
    public string? Notes { get; init; }

    public IReadOnlyList<Guid> SkillTagIds { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SalaryMin.HasValue && SalaryMax.HasValue && SalaryMax < SalaryMin)
        {
            yield return new ValidationResult(
                "SalaryMax must be greater than or equal to SalaryMin.",
                [nameof(SalaryMax), nameof(SalaryMin)]);
        }
    }
}

public sealed record UpdateJobApplicationRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public required string JobTitle { get; init; }

    public required ApplicationStatus Status { get; init; }
    public required WorkMode WorkMode { get; init; }

    [Range(0, 10_000_000)]
    public decimal? SalaryMin { get; init; }

    [Range(0, 10_000_000)]
    public decimal? SalaryMax { get; init; }

    [Url, MaxLength(500)]
    public string? JobPostingUrl { get; init; }

    [MaxLength(4000)]
    public string? Notes { get; init; }

    public IReadOnlyList<Guid> SkillTagIds { get; init; } = [];
}

// Nested Response DTOs
public sealed record CompanyBriefDto(Guid Id, string Name, string? Industry, string? WebsiteUrl);
public sealed record InterviewRoundDto(Guid Id, int RoundNumber, string StageName, DateTimeOffset ScheduledAtUtc);
public sealed record OfferDetailsDto(Guid Id, decimal BaseSalary, decimal? BonusAmount, DateOnly ExpirationDate);
public sealed record SkillTagDto(Guid Id, string Name);

public sealed record JobApplicationDetailDto(
    Guid Id,
    string JobTitle,
    ApplicationStatus Status,
    WorkMode WorkMode,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? JobPostingUrl,
    string? Notes,
    DateTimeOffset AppliedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    CompanyBriefDto Company,
    OfferDetailsDto? Offer,
    IReadOnlyList<InterviewRoundDto> Interviews,
    IReadOnlyList<SkillTagDto> Skills);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static PagedResponse<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<T>(
            items,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            pageNumber > 1,
            pageNumber < totalPages);
    }
}
```

### 4. Manual Mapping & Expression Projection
Prefer static mapping methods and `Expression<Func<TEntity, TDto>>` for EF Core query projection:
```csharp
using System.Linq.Expressions;
using JobTracker.Application.Contracts.Applications;
using JobTracker.Domain.Entities;

namespace JobTracker.Application.Mappings;

public static class JobApplicationMappings
{
    public static Expression<Func<JobApplication, JobApplicationDetailDto>> ToDetailDtoProjection =>
        app => new JobApplicationDetailDto(
            app.Id,
            app.JobTitle,
            app.Status,
            app.WorkMode,
            app.SalaryMin,
            app.SalaryMax,
            app.JobPostingUrl,
            app.Notes,
            app.AppliedAtUtc,
            app.UpdatedAtUtc,
            new CompanyBriefDto(app.Company.Id, app.Company.Name, app.Company.Industry, app.Company.WebsiteUrl),
            app.Offer == null
                ? null
                : new OfferDetailsDto(app.Offer.Id, app.Offer.BaseSalary, app.Offer.BonusAmount, app.Offer.ExpirationDate),
            app.Interviews
                .OrderBy(i => i.RoundNumber)
                .Select(i => new InterviewRoundDto(i.Id, i.RoundNumber, i.StageName, i.ScheduledAtUtc))
                .ToList(),
            app.SkillTags
                .OrderBy(s => s.Name)
                .Select(s => new SkillTagDto(s.Id, s.Name))
                .ToList());
}
```

See [Relationship and DTO Patterns Reference](./references/relationship-and-dto-patterns.md) for Fluent API relationship configurations and AutoMapper profile comparisons.

---

## Common Mistakes to Avoid
1. **Exposing EF Core Entities Directly**: Returning `JobApplication` directly from a Controller causes JSON circular reference crashes (`Company.JobApplications -> JobApplication.Company`) and over-posting vulnerabilities.
2. **Reusing Create DTOs for Updates or Responses**: `Create`, `Update`, and `Response` lifecycles have different required fields, IDs, and audit properties.
3. **Missing Foreign Key Properties**: Defining only navigation properties without explicit foreign key properties (`Guid CompanyId`) makes updates and projections harder.
4. **Unvalidated Enum Inputs**: Accepting integer-backed enums in DTOs without `[EnumDataType(typeof(ApplicationStatus))]` or string enum conversion allows arbitrary out-of-range integer values.

---

## Verification Checklist
- [ ] No EF Core entity class is referenced in a Controller return type or action parameter.
- [ ] Every 1:1, 1:N, and M:N relationship has explicit foreign keys and `DeleteBehavior` rules configured via Fluent API.
- [ ] Nullable reference types (`?`) accurately reflect optional database columns and optional DTO properties.
- [ ] Request and Update DTOs validate string lengths, numeric ranges, URLs, and cross-property invariants.
- [ ] Paginated list endpoints return a consistent `PagedResponse<T>` contract.

---

## Official Documentation References
- [EF Core Relationships Documentation](https://learn.microsoft.com/en-us/ef/core/modeling/relationships)
- [Model Validation in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation)
- [C# Records Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
