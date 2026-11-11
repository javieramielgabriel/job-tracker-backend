using AutoMapper;
using JobTracker.Api.Common.Exceptions;
using JobTracker.Api.Data;
using JobTracker.Api.Mappings;
using JobTracker.Api.Models;
using JobTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JobTracker.Api.Tests.Services;

/// <summary>
/// Unit tests for <see cref="JobApplicationService"/> covering CRUD, phase reconciliation,
/// status transitions, pagination/filtering, and validation.
/// </summary>
public sealed class JobApplicationServiceTests : IDisposable
{
    private readonly JobTrackerDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly JobApplicationService _sut;

    public JobApplicationServiceTests()
    {
        var options = new DbContextOptionsBuilder<JobTrackerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new JobTrackerDbContext(options);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<JobApplicationMappingProfile>();
            cfg.AddProfile<InterviewPhaseMappingProfile>();
        }, NullLoggerFactory.Instance);

        _mapper = mapperConfig.CreateMapper();

        _sut = new JobApplicationService(
            _dbContext,
            _mapper,
            TimeProvider.System,
            NullLogger<JobApplicationService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ActiveStatusWithoutAppliedDate_AutoTimestampsAppliedAtUtc()
    {
        // Arrange
        var request = new JobApplicationCreateDto
        {
            CompanyName = "  Acme Corp  ",
            JobTitle = "  Backend Developer  ",
            Status = ApplicationStatus.Applied,
            InterviewPhases =
            [
                new InterviewPhaseDto { Name = "  Phone Screen  " }
            ]
        };

        // Act
        var created = await _sut.CreateAsync(request);

        // Assert
        Assert.Equal("Acme Corp", created.CompanyName);
        Assert.Equal("Backend Developer", created.JobTitle);
        Assert.NotNull(created.AppliedAtUtc);
        Assert.Single(created.InterviewPhases);
        Assert.Equal("Phone Screen", created.InterviewPhases[0].Name);
    }

    [Fact]
    public async Task CreateAsync_BookmarkedStatusWithoutAppliedDate_LeavesAppliedAtUtcNull()
    {
        // Arrange
        var request = new JobApplicationCreateDto
        {
            CompanyName = "Contoso",
            JobTitle = "Cloud Architect",
            Status = ApplicationStatus.Bookmarked
        };

        // Act
        var created = await _sut.CreateAsync(request);

        // Assert
        Assert.Null(created.AppliedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_ReconcilesExistingNewAndRemovedInterviewPhases()
    {
        // Arrange
        var created = await _sut.CreateAsync(new JobApplicationCreateDto
        {
            CompanyName = "Fabrikam",
            JobTitle = "Software Engineer",
            Status = ApplicationStatus.Applied,
            InterviewPhases =
            [
                new InterviewPhaseDto { Name = "Phase To Keep" },
                new InterviewPhaseDto { Name = "Phase To Remove" }
            ]
        });

        var keptPhaseId = created.InterviewPhases.First(p => p.Name == "Phase To Keep").Id;

        var updateRequest = new JobApplicationUpdateDto
        {
            CompanyName = "Fabrikam Inc.",
            JobTitle = "Senior Software Engineer",
            Status = ApplicationStatus.TechnicalInterview,
            InterviewPhases =
            [
                new InterviewPhaseDto { Id = keptPhaseId, Name = "Phase Kept Updated" },
                new InterviewPhaseDto { Name = "Newly Added Phase" }
            ]
        };

        // Act
        var updated = await _sut.UpdateAsync(created.Id, updateRequest);

        // Assert
        Assert.Equal("Fabrikam Inc.", updated.CompanyName);
        Assert.Equal("Senior Software Engineer", updated.JobTitle);
        Assert.Equal(2, updated.InterviewPhases.Count);
        Assert.Contains(updated.InterviewPhases, p => p.Id == keptPhaseId && p.Name == "Phase Kept Updated");
        Assert.Contains(updated.InterviewPhases, p => p.Name == "Newly Added Phase");
    }

    [Fact]
    public async Task UpdateAsync_UnknownInterviewPhaseId_ThrowsDomainValidationException()
    {
        // Arrange
        var created = await _sut.CreateAsync(new JobApplicationCreateDto
        {
            CompanyName = "Fabrikam",
            JobTitle = "Software Engineer",
            Status = ApplicationStatus.Applied
        });

        var updateRequest = new JobApplicationUpdateDto
        {
            CompanyName = "Fabrikam",
            JobTitle = "Software Engineer",
            Status = ApplicationStatus.Applied,
            InterviewPhases =
            [
                new InterviewPhaseDto { Id = Guid.NewGuid(), Name = "Unknown Phase" }
            ]
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => _sut.UpdateAsync(created.Id, updateRequest));

        Assert.True(exception.Errors.ContainsKey("InterviewPhases[0].Id"));
    }

    [Fact]
    public async Task UpdateStatusAsync_TransitionFromBookmarkedToApplied_AutoTimestampsAppliedAtUtc()
    {
        // Arrange
        var created = await _sut.CreateAsync(new JobApplicationCreateDto
        {
            CompanyName = "Tailspin Toys",
            JobTitle = "Full Stack Developer",
            Status = ApplicationStatus.Bookmarked
        });

        Assert.Null(created.AppliedAtUtc);

        // Act
        var updated = await _sut.UpdateStatusAsync(
            created.Id,
            new JobApplicationStatusUpdateDto { Status = ApplicationStatus.Applied });

        // Assert
        Assert.Equal(ApplicationStatus.Applied, updated.Status);
        Assert.NotNull(updated.AppliedAtUtc);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersAndPaginatesCorrectly()
    {
        // Arrange
        await _sut.CreateAsync(new JobApplicationCreateDto
        {
            CompanyName = "Alpha Tech",
            JobTitle = ".NET Developer",
            Status = ApplicationStatus.Applied
        });
        await _sut.CreateAsync(new JobApplicationCreateDto
        {
            CompanyName = "Beta Systems",
            JobTitle = "Frontend Engineer",
            Status = ApplicationStatus.Bookmarked
        });

        var query = new JobApplicationQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            Status = ApplicationStatus.Applied,
            Search = "Alpha"
        };

        // Act
        var paged = await _sut.GetPagedAsync(query);

        // Assert
        Assert.Equal(1, paged.TotalCount);
        Assert.Single(paged.Items);
        Assert.Equal("Alpha Tech", paged.Items[0].CompanyName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ThrowsEntityNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
