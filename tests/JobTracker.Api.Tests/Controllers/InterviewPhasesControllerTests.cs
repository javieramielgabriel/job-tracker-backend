using JobTracker.Api.Common.Constants;
using JobTracker.Api.Controllers;
using JobTracker.Api.Models;
using JobTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace JobTracker.Api.Tests.Controllers;

/// <summary>
/// Unit tests verifying that <see cref="InterviewPhasesController"/> delegates to
/// <see cref="IInterviewPhaseService"/> and returns standard HTTP responses.
/// </summary>
public sealed class InterviewPhasesControllerTests
{
    private readonly Mock<IInterviewPhaseService> _serviceMock = new();
    private readonly InterviewPhasesController _sut;

    public InterviewPhasesControllerTests()
    {
        _sut = new InterviewPhasesController(_serviceMock.Object);
    }

    [Fact]
    public async Task CreatePhaseAsync_ReturnsCreatedAtRoute()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var request = new InterviewPhaseDto { Name = "Technical Round" };
        var createdDto = new InterviewPhaseReadDto(Guid.NewGuid(), "Technical Round", null, null);

        _serviceMock
            .Setup(s => s.CreatePhaseAsync(applicationId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _sut.CreatePhaseAsync(applicationId, request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal(ApiConstants.RouteNames.GetInterviewPhaseById, createdResult.RouteName);
        Assert.Equal(applicationId, createdResult.RouteValues?["applicationId"]);
        Assert.Equal(createdDto.Id, createdResult.RouteValues?["phaseId"]);
        Assert.Same(createdDto, createdResult.Value);
    }

    [Fact]
    public async Task DeletePhaseAsync_ReturnsNoContent()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var phaseId = Guid.NewGuid();

        // Act
        var result = await _sut.DeletePhaseAsync(applicationId, phaseId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _serviceMock.Verify(s => s.DeletePhaseAsync(applicationId, phaseId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
