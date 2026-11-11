namespace JobTracker.Api.Models;

/// <summary>
/// Represents the lifecycle stage of a job application.
/// </summary>
public enum ApplicationStatus
{
    Bookmarked,
    Applied,
    PhoneScreen,
    TechnicalInterview,
    OfferReceived,
    Rejected,
    Withdrawn
}
