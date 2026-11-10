# xUnit Builders, Fixtures & Integration Testing Reference

## 1. Test Data Builder Pattern
Use fluent builders with sensible defaults so tests only specify properties relevant to the scenario under test:

```csharp
using JobTracker.Domain.Entities;

namespace JobTracker.UnitTests.Builders;

public sealed class JobApplicationBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _jobTitle = "Senior Backend Engineer (.NET)";
    private ApplicationStatus _status = ApplicationStatus.Applied;
    private WorkMode _workMode = WorkMode.Remote;
    private Guid _companyId = Guid.NewGuid();
    private Company? _company;

    public JobApplicationBuilder WithId(Guid id) { _id = id; return this; }
    public JobApplicationBuilder WithTitle(string title) { _jobTitle = title; return this; }
    public JobApplicationBuilder WithStatus(ApplicationStatus status) { _status = status; return this; }
    public JobApplicationBuilder WithCompany(Company company)
    {
        _company = company;
        _companyId = company.Id;
        return this;
    }

    public JobApplication Build()
    {
        var company = _company ?? new Company { Id = _companyId, Name = "Acme Corp" };
        return new JobApplication
        {
            Id = _id,
            JobTitle = _jobTitle,
            Status = _status,
            WorkMode = _workMode,
            CompanyId = company.Id,
            Company = company,
            AppliedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
```

---

## 2. API Integration Testing with `WebApplicationFactory<Program>`
```csharp
using System.Net;
using System.Net.Http.Json;
using JobTracker.Application.Contracts.Applications;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JobTracker.IntegrationTests.Endpoints;

public sealed class JobApplicationsEndpointTests(
    WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_InvalidSalaryRange_Returns400ValidationProblemDetails()
    {
        // Arrange
        var invalidPayload = new CreateJobApplicationRequest
        {
            JobTitle = "Staff .NET Architect",
            CompanyId = Guid.NewGuid(),
            SalaryMin = 200_000m,
            SalaryMax = 100_000m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/job-applications", invalidPayload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
```
