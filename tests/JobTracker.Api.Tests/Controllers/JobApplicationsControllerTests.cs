using JobTracker.Api.Common.Constants;
using JobTracker.Api.Controllers;
using JobTracker.Api.Models;
using JobTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace JobTracker.Api.Tests.Controllers;

/// <summary>
/// Unit tests verifying that <see cref="JobApplicationsController"/> stays lightweight,
/// delegates to <see cref="IJobApplicationService"/>, and returns expected HTTP status codes.
/// </summary>
public sealed class JobApplicationsControllerTests
{
    private readonly Mock<IJobApplicationService> _serviceMock = new();
    private readonly JobApplicationsController _sut;

    public JobApplicationsControllerTests()
    {
        _sut = new JobApplicationsController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOkWithApplications()
    {
        // Arrange
        IReadOnlyList<JobApplicationReadDto> expected =
        [
            new JobApplicationReadDto(
                Guid.NewGuid(),
                "Acme",
                "Engineer",
                "Remote",
                null,
                ApplicationStatus.Applied,
                DateTimeOffset.UtcNow,
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                [])
        ];

        _serviceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetAllAsync(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, okResult.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedAtRouteWithNewApplication()
    {
        // Arrange
        var request = new JobApplicationCreateDto
        {
            CompanyName = "Acme",
            JobTitle = "Engineer"
        };

        var createdDto = new JobApplicationReadDto(
            Guid.NewGuid(),
            "Acme",
            "Engineer",
            null,
            null,
            ApplicationStatus.Bookmarked,
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            []);

        _serviceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal(ApiConstants.RouteNames.GetJobApplicationById, createdResult.RouteName);
        Assert.Equal(createdDto.Id, createdResult.RouteValues?["id"]);
        Assert.Same(createdDto, createdResult.Value);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteAsync(id, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _serviceMock.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
