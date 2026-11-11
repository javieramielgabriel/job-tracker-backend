using AutoMapper;
using JobTracker.Api.Mappings;
using JobTracker.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JobTracker.Api.Tests.Mappings;

/// <summary>
/// Unit tests verifying AutoMapper profile configurations and entity/DTO transformations.
/// </summary>
public sealed class AutoMapperConfigurationTests
{
    private readonly MapperConfiguration _configuration;
    private readonly IMapper _mapper;

    public AutoMapperConfigurationTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<JobApplicationMappingProfile>();
            cfg.AddProfile<InterviewPhaseMappingProfile>();
        }, NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void AutoMapper_Configuration_IsValid()
    {
        // Act & Assert
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_JobApplicationCreateDto_To_JobApplication_TrimsStringsAndMapsPhases()
    {
        // Arrange
        var request = new JobApplicationCreateDto
        {
            CompanyName = "  Acme Corp  ",
            JobTitle = "  Senior Backend Engineer  ",
            Location = "  Remote  ",
            JobUrl = "https://acme.example.com/jobs/1",
            Status = ApplicationStatus.Applied,
            Notes = "Initial application notes",
            InterviewPhases =
            [
                new InterviewPhaseDto
                {
                    Name = "  Recruiter Screen  ",
                    ScheduledAtUtc = new DateTimeOffset(2026, 11, 15, 10, 0, 0, TimeSpan.Zero),
                    Notes = "30-minute call"
                }
            ]
        };

        // Act
        var entity = _mapper.Map<JobApplication>(request);

        // Assert
        Assert.Equal("Acme Corp", entity.CompanyName);
        Assert.Equal("Senior Backend Engineer", entity.JobTitle);
        Assert.Equal("Remote", entity.Location);
        Assert.Equal(ApplicationStatus.Applied, entity.Status);
        Assert.Single(entity.InterviewPhases);
        Assert.Equal("Recruiter Screen", entity.InterviewPhases.First().Name);
    }

    [Fact]
    public void Map_JobApplication_To_JobApplicationReadDto_OrdersInterviewPhasesByScheduledAtUtcThenId()
    {
        // Arrange
        var laterId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var earlierId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            CompanyName = "Contoso",
            JobTitle = "Platform Engineer",
            Status = ApplicationStatus.TechnicalInterview,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            InterviewPhases =
            [
                new InterviewPhase
                {
                    Id = laterId,
                    Name = "System Design",
                    ScheduledAtUtc = new DateTimeOffset(2026, 11, 20, 14, 0, 0, TimeSpan.Zero)
                },
                new InterviewPhase
                {
                    Id = earlierId,
                    Name = "Phone Screen",
                    ScheduledAtUtc = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero)
                }
            ]
        };

        // Act
        var dto = _mapper.Map<JobApplicationReadDto>(application);

        // Assert
        Assert.Equal(2, dto.InterviewPhases.Count);
        Assert.Equal("Phone Screen", dto.InterviewPhases[0].Name);
        Assert.Equal("System Design", dto.InterviewPhases[1].Name);
    }
}
