---
name: dotnet-test-mocking
description: >-
  Use this skill when creating, configuring, or refactoring test mocks, stubs,
  and fakes in .NET unit tests using Moq or NSubstitute. Covers mocking
  interfaces, repositories, services, external HTTP clients (HttpMessageHandler),
  IConfiguration/IOptions, ILogger, async methods, reusable mock factories, and
  knowing when to use real objects instead of mocks.
---

# Test Mocking and Mock Generation (`dotnet-test-mocking`)

## Overview
Provides structured methodologies and code patterns for isolating units under test in the **Job Tracker Backend** using **Moq** (and **NSubstitute** where preferred), while preventing brittle, over-mocked tests.

## Activation Instructions
Activate this skill whenever you are:
- Creating unit tests for classes that depend on external interfaces (repositories, external HTTP APIs, message buses, file systems, clocks).
- Setting up mock return values, async flows, sequential responses, callbacks, or exception simulations using Moq or NSubstitute.
- Mocking `HttpClient` (`HttpMessageHandler`), `IOptions<T>`, or `ILogger<T>`.
- Deciding whether a dependency should be mocked or instantiated with a real/fake implementation.

## Specific Responsibilities
1. **Identify What Should (and Should Not) Be Mocked**:
   - **DO Mock**: Out-of-process boundaries (`IJobApplicationRepository`, external HTTP clients, email/notification gateways, payment/auth providers).
   - **DO NOT Mock**: Pure domain entities, DTOs, value objects, mappers, `Options.Create(...)`, `NullLogger<T>.Instance`, or `FakeTimeProvider`.
2. **Configure Deterministic Async Behavior**: Always use `.ReturnsAsync(...)` / `.ThrowsAsync(...)` and support `It.IsAny<CancellationToken>()`.
3. **Verify Only Meaningful Interactions**: Verify state-changing calls (e.g., `SaveChangesAsync`, external notification dispatch) rather than every read-only getter invocation.
4. **Encapsulate Complex Mock Setup**: Use reusable Mock Factories or Test Fixtures when multiple test classes share complex dependency graphs.

---

## Technology-Specific Implementation Guidance

### 1. Mocking Repositories & Async Methods with Moq
```csharp
using JobTracker.Application.Interfaces;
using JobTracker.Domain.Entities;
using Moq;
using Xunit;

public sealed class RepositoryMockExamples
{
    [Fact]
    public async Task CreateApplication_WhenCompanyExists_PersistsAndReturnsDto()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var repoMock = new Mock<IJobApplicationRepository>(MockBehavior.Default);

        repoMock
            .Setup(r => r.CompanyExistsAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        JobApplication? capturedEntity = null;
        repoMock
            .Setup(r => r.AddAsync(It.IsAny<JobApplication>(), It.IsAny<CancellationToken>()))
            .Callback<JobApplication, CancellationToken>((entity, _) => capturedEntity = entity)
            .Returns(Task.CompletedTask);

        repoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act & Assert against capturedEntity and Verify SaveChangesAsync
        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtMostOnce);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDatabaseFails_PropagatesException()
    {
        var repoMock = new Mock<IJobApplicationRepository>();
        repoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection lost."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repoMock.Object.SaveChangesAsync(CancellationToken.None));
    }
}
```

### 2. Mocking `HttpClient` via `HttpMessageHandler`
`HttpClient` itself is not an interface and should **not** be mocked directly. Instead, mock `HttpMessageHandler` (`Protected()` in `Moq.Protected`) and pass it to `new HttpClient(handlerMock.Object)`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;

public static class HttpClientMockFactory
{
    public static HttpClient CreateJsonClient<TPayload>(
        HttpStatusCode statusCode,
        TPayload responsePayload,
        string baseAddress = "https://api.company-enrichment.local")
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = JsonContent.Create(responsePayload)
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(baseAddress)
        };
    }
}
```

### 3. Handling `IConfiguration`, `IOptions<T>`, and `ILogger<T>`
Prefer real lightweight wrappers over mocking framework plumbing:
- **`IOptions<T>`**: Use `Options.Create(new JwtSettings { ... })` from `Microsoft.Extensions.Options`.
- **`IConfiguration`**: Use `new ConfigurationBuilder().AddInMemoryCollection(dict).Build()`.
- **`ILogger<T>`**: Use `NullLogger<T>.Instance` unless you specifically must assert a security/audit log was emitted.

See [Moq and NSubstitute Patterns Reference](./references/moq-and-nsubstitute-patterns.md) for side-by-side Moq vs. NSubstitute syntax, logger verification helpers, and reusable mock factories.

---

## Common Mistakes to Avoid
1. **Mocking Value Types, Entities, or `HttpClient` Directly**: Moq cannot override non-virtual methods on sealed or concrete classes like `HttpClient`. Always mock the underlying abstraction (`HttpMessageHandler` or a custom interface).
2. **Over-Verification (`VerifyAll` on every test)**: Asserting exact internal call sequences makes refactoring painful even when observable behavior hasn't changed.
3. **Mocking `IOptions<T>` or `IConfiguration` with Moq**: Using `Options.Create(...)` and `AddInMemoryCollection(...)` is faster, cleaner, and less error-prone.
4. **Mocking `DbContext` Directly**: Mocking `DbSet<T>` and `IQueryable<T>` with Moq hides LINQ-to-SQL translation bugs. Either mock a focused repository interface for unit tests or use a real test database for data-access tests.

---

## Verification Checklist
- [ ] Only true external/architectural dependencies are mocked; value objects, entities, and `IOptions<T>` use real instances.
- [ ] All async mock setups use `.ReturnsAsync(...)` or `.ThrowsAsync(...)` and accept `It.IsAny<CancellationToken>()`.
- [ ] `HttpClient` dependencies are tested by mocking `HttpMessageHandler`.
- [ ] Failure scenarios (timeouts, `HttpRequestException`, `OperationCanceledException`, database errors) are simulated and tested.

---

## Official Documentation References
- [Moq GitHub Repository & Quickstart](https://github.com/devlooped/moq)
- [NSubstitute Official Documentation](https://nsubstitute.github.io/)
- [Unit Testing Best Practices in .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
