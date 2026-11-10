---
name: dotnet-unit-testing
description: >-
  Use this skill when writing, organizing, or reviewing .NET backend unit tests
  and integration tests using xUnit (or NUnit where appropriate). Covers test
  project structure, Arrange-Act-Assert, Fact/Theory/InlineData/MemberData,
  service/controller/business-logic/validation/exception/async testing, test
  fixtures, test data builders, and WebApplicationFactory integration testing.
---

# .NET Unit Testing (`dotnet-unit-testing`)

## Overview
Provides comprehensive standards and patterns for writing deterministic, fast, behavior-focused unit and integration tests for the **Job Tracker Backend** using **xUnit**, **Microsoft.NET.Test.Sdk**, and **`WebApplicationFactory<TEntryPoint>`**.

## Activation Instructions
Activate this skill whenever you are:
- Creating or updating unit test or integration test projects (`tests/JobTracker.UnitTests`, `tests/JobTracker.IntegrationTests`).
- Writing tests for domain entities, application services, validators, controllers, minimal APIs, or repositories.
- Identifying missing test cases across success paths, failure paths, edge cases, boundary values, null inputs, and asynchronous execution.

## Specific Responsibilities
1. **Test Observable Behavior, Not Implementation Details**: Assert returned values, state transitions, thrown domain exceptions, and externally visible side effects rather than internal private methods.
2. **Follow Naming & Structure Conventions**: Name test methods `MethodName_Scenario_ExpectedResult` and structure every test clearly with `// Arrange`, `// Act`, and `// Assert`.
3. **Cover All Critical Scenarios**: Write tests for happy paths, not-found/failure paths, boundary conditions, invalid/null inputs, business rule violations, cancellation tokens, and async exceptions.
4. **Use Builders & Fixtures for Maintainability**: Use the Test Data Builder pattern (`JobApplicationBuilder`) and xUnit fixtures (`IClassFixture<T>`) to keep test setup concise and readable.

---

## Technology-Specific Implementation Guidance

### 1. Recommended Test Project Structure
```text
tests/
├── JobTracker.UnitTests/
│   ├── Builders/
│   │   └── JobApplicationBuilder.cs
│   ├── Domain/
│   │   └── SalaryRangeTests.cs
│   ├── Services/
│   │   └── JobApplicationServiceTests.cs
│   ├── Validation/
│   │   └── CreateJobApplicationRequestValidationTests.cs
│   └── Controllers/
│       └── JobApplicationsControllerTests.cs
└── JobTracker.IntegrationTests/
    ├── Fixtures/
    │   └── CustomWebApplicationFactory.cs
    └── Endpoints/
        └── JobApplicationsEndpointTests.cs
```

### 2. Service & Business Logic Unit Testing (`[Fact]`, `[Theory]`, `[InlineData]`)
```csharp
using JobTracker.Application.Applications;
using JobTracker.Domain.Entities;
using JobTracker.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace JobTracker.UnitTests.Services;

public sealed class JobApplicationServiceTests
{
    private readonly Mock<IJobApplicationRepository> _repositoryMock = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly JobApplicationService _sut;

    public JobApplicationServiceTests()
    {
        _sut = new JobApplicationService(
            _repositoryMock.Object,
            NullLogger<JobApplicationService>.Instance,
            _timeProvider);
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Interviewing)]
    [InlineData(ApplicationStatus.Interviewing, ApplicationStatus.Offered)]
    [InlineData(ApplicationStatus.Offered, ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Withdrawn)]
    public async Task TransitionStatusAsync_ValidTransition_UpdatesStatusAndSaves(
        ApplicationStatus initialStatus,
        ApplicationStatus targetStatus)
    {
        // Arrange
        var application = new JobApplicationBuilder()
            .WithStatus(initialStatus)
            .Build();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        // Act
        var result = await _sut.TransitionStatusAsync(application.Id, targetStatus, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetStatus, result.Status);
        Assert.Equal(_timeProvider.GetUtcNow(), application.UpdatedAtUtc);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft, ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Interviewing)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Withdrawn)]
    public async Task TransitionStatusAsync_InvalidTransition_ThrowsInvalidStatusTransitionException(
        ApplicationStatus initialStatus,
        ApplicationStatus targetStatus)
    {
        // Arrange
        var application = new JobApplicationBuilder()
            .WithStatus(initialStatus)
            .Build();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidStatusTransitionException>(
            () => _sut.TransitionStatusAsync(application.Id, targetStatus, CancellationToken.None));

        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransitionStatusAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobApplication?)null);

        // Act
        var result = await _sut.TransitionStatusAsync(missingId, ApplicationStatus.Interviewing);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

### 3. Validation Testing
```csharp
using System.ComponentModel.DataAnnotations;
using JobTracker.Application.Contracts.Applications;
using Xunit;

namespace JobTracker.UnitTests.Validation;

public sealed class CreateJobApplicationRequestValidationTests
{
    [Fact]
    public void Validate_SalaryMaxLessThanSalaryMin_ReturnsValidationError()
    {
        // Arrange
        var request = new CreateJobApplicationRequest
        {
            JobTitle = "Senior .NET Engineer",
            CompanyId = Guid.NewGuid(),
            SalaryMin = 150_000m,
            SalaryMax = 120_000m
        };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateJobApplicationRequest.SalaryMax)));
    }
}
```

See [xUnit and Integration Testing Reference](./references/xunit-and-integration-testing.md) for Test Data Builders, `WebApplicationFactory` API integration tests, and database testing strategies.

---

## CLI Commands
```powershell
# Run all unit and integration tests
dotnet test

# Run tests filtered by class or trait
dotnet test --filter "FullyQualifiedName~JobApplicationServiceTests"

# Run tests with detailed console output
dotnet test --logger "console;verbosity=detailed"
```

---

## Common Mistakes to Avoid
1. **Using `DateTime.UtcNow` Directly in Tested Code**: Makes time-sensitive tests flaky. Inject `TimeProvider` and use `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) in tests.
2. **Async Void or Unawaited `Assert.ThrowsAsync`**: Always `await Assert.ThrowsAsync<T>(() => ...)`—forgetting `await` causes the test to pass even when no exception is thrown!
3. **Sharing Mutable State Across Tests**: Never store mutable entities in `static` fields shared across parallel xUnit test classes.
4. **Testing EF Core In-Memory as a Substitute for Relational SQL**: The EF Core In-Memory provider ignores relational constraints, transactions, and SQL translation differences. Use SQLite in-memory or Testcontainers for relational database integration tests.

---

## Verification Checklist
- [ ] Every test follows the `Arrange - Act - Assert` pattern and has a single logical reason to fail.
- [ ] Parameterized scenarios use `[Theory]` with `[InlineData]`, `[MemberData]`, or `[ClassData]`.
- [ ] Both happy paths and edge/error cases (nulls, missing entities, invalid transitions, exceptions) are tested.
- [ ] All async assertions (`Assert.ThrowsAsync`) are properly `await`ed.
- [ ] `dotnet test` passes with zero warnings and zero skipped tests unless explicitly documented.

---

## Official Documentation References
- [xUnit.net Official Documentation](https://xunit.net/)
- [Unit Testing C# in .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/)
- [Integration Tests in ASP.NET Core (`WebApplicationFactory`)](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
