using JobTracker.Api.Common.Validation;
using JobTracker.Api.Models;
using Xunit;

namespace JobTracker.Api.Tests.Common;

/// <summary>
/// Unit tests for the reusable <see cref="RequestValidator"/> utility.
/// </summary>
public sealed class RequestValidatorTests
{
    [Fact]
    public void ValidateWithCollection_DuplicatePhaseIds_ReturnsValidationError()
    {
        // Arrange
        var duplicateId = Guid.NewGuid();
        var request = new JobApplicationUpdateDto
        {
            CompanyName = "Acme",
            JobTitle = "Developer",
            Status = ApplicationStatus.Applied,
            InterviewPhases =
            [
                new InterviewPhaseDto { Id = duplicateId, Name = "Round 1" },
                new InterviewPhaseDto { Id = duplicateId, Name = "Round 2" }
            ]
        };

        // Act
        var errors = RequestValidator.ValidateWithCollection(
            request,
            request.InterviewPhases,
            nameof(JobApplicationUpdateDto.InterviewPhases),
            phase => phase.Id,
            validateUniqueIds: true,
            duplicateIdMessagePrefix: "Duplicate interview phase ID");

        // Assert
        Assert.True(errors.ContainsKey("InterviewPhases[1].Id"));
        Assert.Contains($"Duplicate interview phase ID '{duplicateId}'.", errors["InterviewPhases[1].Id"]);
    }

    [Fact]
    public void ValidateWithCollection_InvalidNestedPhaseName_ReturnsIndexedMemberError()
    {
        // Arrange
        var request = new JobApplicationCreateDto
        {
            CompanyName = "Acme",
            JobTitle = "Developer",
            InterviewPhases =
            [
                new InterviewPhaseDto { Name = string.Empty }
            ]
        };

        // Act
        var errors = RequestValidator.ValidateWithCollection(
            request,
            request.InterviewPhases,
            nameof(JobApplicationCreateDto.InterviewPhases));

        // Assert
        Assert.True(errors.ContainsKey("InterviewPhases[0].Name"));
    }
}
