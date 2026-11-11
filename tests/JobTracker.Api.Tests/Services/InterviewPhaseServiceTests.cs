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
/// Unit tests for <see cref="InterviewPhaseService"/> covering interview phase CRUD and error handling.
/// </summary>
public sealed class InterviewPhaseServiceTests : IDisposable
{
    private readonly JobTrackerDbContext _dbContext;
    private readonly JobApplicationService _applicationService;
    private readonly InterviewPhaseService _sut;

    public InterviewPhaseServiceTests()
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

        var mapper = mapperConfig.CreateMapper();

        _applicationService = new JobApplicationService(
            _dbContext,
            mapper,
            TimeProvider.System,
            NullLogger<JobApplicationService>.Instance);

        _sut = new InterviewPhaseService(
            _dbContext,
            mapper,
            TimeProvider.System,
            NullLogger<InterviewPhaseService>.Instance);
    }

    [Fact]
    public async Task CreateUpdateAndDeletePhaseAsync_ManagesPhaseLifecycleCorrectly()
    {
        // Arrange
        var app = await _applicationService.CreateAsync(new JobApplicationCreateDto
        {
            CompanyName = "Northwind",
            JobTitle = "API Engineer",
            Status = ApplicationStatus.Applied
        });

        // Act 1: Create Phase
        var createdPhase = await _sut.CreatePhaseAsync(app.Id, new InterviewPhaseDto
        {
            Name = "  Technical Screen  ",
            Notes = "Initial notes"
        });

        // Assert 1
        Assert.Equal("Technical Screen", createdPhase.Name);

        // Act 2: Update Phase
        var updatedPhase = await _sut.UpdatePhaseAsync(app.Id, createdPhase.Id, new InterviewPhaseDto
        {
            Name = "Final System Design",
            Notes = "Updated notes"
        });

        // Assert 2
        Assert.Equal("Final System Design", updatedPhase.Name);
        Assert.Equal("Updated notes", updatedPhase.Notes);

        // Act 3: Delete Phase
        await _sut.DeletePhaseAsync(app.Id, createdPhase.Id);
        var remainingPhases = await _sut.GetPhasesAsync(app.Id);

        // Assert 3
        Assert.Empty(remainingPhases);
    }

    [Fact]
    public async Task GetPhaseByIdAsync_MissingPhaseOnExistingApplication_ThrowsPhaseNotFoundException()
    {
        // Arrange
        var app = await _applicationService.CreateAsync(new JobApplicationCreateDto
        {
            CompanyName = "Northwind",
            JobTitle = "API Engineer"
        });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _sut.GetPhaseByIdAsync(app.Id, Guid.NewGuid()));

        Assert.Equal("Interview phase not found", ex.Title);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
