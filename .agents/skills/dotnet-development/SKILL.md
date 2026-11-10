---
name: dotnet-development
description: >-
  Use this skill when writing, refactoring, reviewing, or architecting C# and
  ASP.NET Core backend code on .NET 10 (or the installed .NET SDK). Covers C#
  coding conventions, OOP, SOLID, Clean Architecture, Dependency Injection,
  Minimal APIs and Controllers, Middleware, Options Pattern, Logging, Exception
  Handling, Async/Await, LINQ, Validation, Security, and modern C# 12/13/14
  language features.
---

# C# and .NET Development (`dotnet-development`)

## Overview
Provides architectural standards, modern C# implementation patterns, and ASP.NET Core engineering practices tailored for the **Job Tracker Backend** running on **.NET 10 (`10.0.300`)** and C# 13/14.

## Activation Instructions
Activate this skill whenever you are:
- Creating new C# classes, interfaces, services, controllers, or minimal API endpoints.
- Designing or refactoring backend application layers, dependency injection registrations, middleware pipelines, or configuration bindings.
- Reviewing C# code for performance, security, async/await correctness, LINQ efficiency, or adherence to SOLID and Clean Code principles.

## Specific Responsibilities
1. **Enforce C# Coding Conventions & Nullable Safety**: Enable `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`, follow PascalCase for public members/types, `_camelCase` for private fields, and prefix interfaces with `I`.
2. **Apply SOLID, DRY, and KISS Pragmatically**: Structure code with clear Separation of Concerns without over-abstracting. Introduce Repository, Unit of Work, CQRS, or MediatR **only** when justified by domain complexity or existing project conventions.
3. **Leverage Modern C# Language Features**: Use primary constructors, records, pattern matching, switch expressions, collection expressions (`[...]`), `required` members, and `IAsyncEnumerable<T>` where supported by the installed .NET 10 SDK.
4. **Configure Robust ASP.NET Core Pipelines**: Use strongly typed configuration (`IOptions<T>` / `IOptionsSnapshot<T>`), structured logging (`ILogger<T>` / `[LoggerMessage]`), centralized `IExceptionHandler` with `ProblemDetails`, and proper service lifetimes (`Transient`, `Scoped`, `Singleton`).

---

## Technology-Specific Implementation Guidance

### 1. Clean Architecture & Separation of Concerns
Organize backend responsibilities into well-defined boundaries:
- **API / Presentation Layer**: Controllers or Minimal API route groups, request/response DTO mapping, authentication/authorization policies, middleware, and OpenAPI metadata.
- **Application / Service Layer**: Business orchestration, validation, use-case coordination, and transaction boundaries.
- **Domain Layer**: Core domain entities, value objects, domain enums, domain exceptions, and business invariants.
- **Infrastructure / Persistence Layer**: EF Core `DbContext`, entity configurations, external HTTP clients, file storage, and background workers.

### 2. Dependency Injection & Service Lifetimes
- **`Scoped`**: Default for EF Core `DbContext`, repositories, and business services operating within an HTTP request scope.
- **`Singleton`**: Stateless services, caches, configuration holders, `TimeProvider`. **Never inject a `Scoped` service into a `Singleton`** (captive dependency).
- **`Transient`**: Lightweight, stateless validators or mappers.
- Group DI registrations in clean extension methods (`AddApplicationServices()`, `AddInfrastructureServices(IConfiguration)`).

### 3. Configuration & Options Pattern
Bind configuration sections using the Options Pattern with startup validation:
```csharp
public sealed class JwtSettings
{
    public const string SectionName = "Authentication:Jwt";

    [Required, MinLength(32)]
    public required string SigningKey { get; init; }

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }

    [Range(5, 1440)]
    public int ExpirationMinutes { get; init; } = 60;
}

// Program.cs registration
builder.Services
    .AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 4. Modern C# Language Features in Action
Use Primary Constructors, Records, Switch Expressions, Pattern Matching, Collection Expressions, and Async Streams:
```csharp
namespace JobTracker.Application.Applications;

public sealed class JobApplicationService(
    IJobApplicationRepository repository,
    ILogger<JobApplicationService> logger,
    TimeProvider timeProvider) : IJobApplicationService
{
    public async Task<ApplicationSummaryDto?> TransitionStatusAsync(
        Guid applicationId,
        ApplicationStatus targetStatus,
        CancellationToken ct = default)
    {
        var application = await repository.GetByIdAsync(applicationId, ct);
        if (application is null)
        {
            return null;
        }

        // Pattern matching & switch expression for state transition validation
        var canTransition = (application.Status, targetStatus) switch
        {
            (ApplicationStatus.Draft, ApplicationStatus.Applied) => true,
            (ApplicationStatus.Applied, ApplicationStatus.Interviewing or ApplicationStatus.Rejected) => true,
            (ApplicationStatus.Interviewing, ApplicationStatus.Offered or ApplicationStatus.Rejected) => true,
            (ApplicationStatus.Offered, ApplicationStatus.Accepted or ApplicationStatus.Declined) => true,
            (_, ApplicationStatus.Withdrawn) when application.Status is not ApplicationStatus.Rejected => true,
            _ => false
        };

        if (!canTransition)
        {
            throw new InvalidStatusTransitionException(application.Status, targetStatus);
        }

        application.UpdateStatus(targetStatus, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(ct);

        LogStatusTransitioned(logger, applicationId, targetStatus);
        return application.ToSummaryDto();
    }

    public async IAsyncEnumerable<ApplicationSummaryDto> StreamActiveApplicationsAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        IReadOnlyList<ApplicationStatus> activeStatuses =
        [
            ApplicationStatus.Applied,
            ApplicationStatus.Interviewing,
            ApplicationStatus.Offered
        ];

        await foreach (var item in repository.StreamByStatusesAsync(activeStatuses, ct))
        {
            yield return item.ToSummaryDto();
        }
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Job application {ApplicationId} transitioned to {TargetStatus}")]
    private static partial void LogStatusTransitioned(
        ILogger logger, Guid applicationId, ApplicationStatus targetStatus);
}
```

### 5. Middleware, Global Exception Handling & ProblemDetails
Implement `IExceptionHandler` (.NET 8+) with `AddProblemDetails()` instead of ad-hoc `try/catch` blocks in controllers:
```csharp
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred for {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var (statusCode, title) = exception switch
        {
            InvalidStatusTransitionException => (StatusCodes.Status409Conflict, "Invalid Status Transition"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.ProblemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred. Please contact support with the trace ID."
                    : exception.Message,
                Instance = httpContext.Request.Path
            }
        });

        return true;
    }
}
```

### 6. Async/Await, LINQ, Performance & Security Best Practices
- **Async All the Way**: Always accept and forward `CancellationToken` in async API actions, service methods, and EF Core calls. Never block async code with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- **LINQ Efficiency**: Keep filtering (`Where`), sorting (`OrderBy`), and projection (`Select`) inside `IQueryable<T>` before materializing with `ToListAsync(ct)`. Avoid client-side evaluation of large datasets.
- **Security**: Never log secrets, JWTs, connection strings, or PII. Enforce HTTPS redirection, CORS allowlists, parameterized queries, and resource-based authorization.

See [Modern C# and Architecture Reference](./references/modern-csharp-and-architecture.md) for deep-dive examples on Middleware, Authentication/Authorization, OpenAPI configuration, and performance patterns.

---

## CLI Commands
```powershell
# Check installed .NET SDK version
dotnet --version

# Build the solution in Release configuration with warnings as errors
dotnet build --configuration Release /warnaserror

# Format and verify C# code style
dotnet format --verify-no-changes

# Run the ASP.NET Core Web API project
dotnet run --project src/JobTracker.Api/JobTracker.Api.csproj
```

---

## Common Mistakes to Avoid
1. **Captive Dependencies**: Injecting a `Scoped` service (like `DbContext`) into a `Singleton` service, causing concurrency bugs and stale state.
2. **Sync-over-Async**: Calling `.Result` or `.Wait()` on tasks, which causes thread pool starvation under load.
3. **Swallowing Cancellation**: Omitting `CancellationToken` parameters on async endpoints and database queries.
4. **Leaking Stack Traces**: Returning raw exception details (`ex.ToString()`) in production `ProblemDetails` responses.
5. **String Interpolation in Logs**: Using `logger.LogInformation($"Created {id}")` instead of structured message templates `logger.LogInformation("Created {ApplicationId}", id)` or `[LoggerMessage]` source generators.
6. **Over-Engineering**: Adding MediatR, CQRS, and generic repositories to simple CRUD modules when direct service/DbContext usage is cleaner.

---

## Verification Checklist
- [ ] Code compiles cleanly with `<Nullable>enable</Nullable>` and zero compiler warnings.
- [ ] All async methods accept `CancellationToken`, pass it downstream, and end with the `Async` suffix.
- [ ] DI lifetimes (`Singleton`, `Scoped`, `Transient`) are appropriate and free of captive dependencies.
- [ ] Configuration uses strongly typed Options classes with `ValidateOnStart()`.
- [ ] Logging uses structured templates or `[LoggerMessage]` source generation without logging sensitive data.
- [ ] Exceptions map to standard RFC 9457 `ProblemDetails` status codes without leaking internal stack traces.

---

## Official Documentation References
- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [C# Language Guide & What's New](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [.NET Dependency Injection Guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
- [ASP.NET Core Performance Best Practices](https://learn.microsoft.com/en-us/aspnet/core/performance/performance-best-practices)
