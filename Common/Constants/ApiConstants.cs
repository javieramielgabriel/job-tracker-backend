namespace JobTracker.Api.Common.Constants;

/// <summary>
/// Centralized constants for API routes, policies, database configuration, and validation limits.
/// </summary>
public static class ApiConstants
{
    public const string DatabaseSchema = "JobTracker";
    public const string DatabaseConnectionStringName = "JobTrackerDb";
    public const string AngularCorsPolicy = "AngularFrontend";

    public static class Routes
    {
        public const string JobApplications = "api/job-applications";
        public const string InterviewPhases = "api/job-applications/{applicationId:guid}/interview-phases";
    }

    public static class RouteNames
    {
        public const string GetJobApplicationById = "GetJobApplicationById";
        public const string GetInterviewPhaseById = "GetInterviewPhaseById";
    }

    public static class Validation
    {
        public const int MaxCompanyNameLength = 200;
        public const int MaxJobTitleLength = 200;
        public const int MaxLocationLength = 150;
        public const int MaxJobUrlLength = 2048;
        public const int MaxApplicationNotesLength = 4000;
        public const int MaxStatusLength = 32;

        public const int MaxPhaseNameLength = 100;
        public const int MaxPhaseNotesLength = 2000;
        public const int MaxSearchLength = 200;
    }

    public static class Pagination
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
    }
}
