# Job Tracker Backend

A production-ready REST API built with ASP.NET Core MVC for managing job applications and their interview phases. Applications include company and job details, status, optional notes, and application dates.

## Architecture & Key Features

- **MVC Architecture**: Clean separation between Models (`Entities`, `Dtos`, `Enums`, `Common`), Controllers (`JobApplicationsController`, `InterviewPhasesController`), Services (`JobApplicationService`, `InterviewPhaseService`), Data Access (`JobTrackerDbContext`, `IEntityTypeConfiguration<T>`), and Centralized Mapping (`AutoMapper` profiles).
- **Full Job Application Lifecycle**: Create, list, filter/paginate (`GET /api/job-applications/paged`), retrieve, update, and delete job applications.
- **Quick Status Transitions**: Update application status via `PATCH /api/job-applications/{id}/status` with automatic `AppliedAtUtc` timestamping on active status transitions.
- **Interview Phase Management**: Manage interview phases via dedicated sub-resource endpoints (`/api/job-applications/{applicationId}/interview-phases`) or reconcile them in-place on `PUT /api/job-applications/{id}`.
- **Concurrency & Standardized Errors**: PostgreSQL `xmin` optimistic concurrency protection and centralized `GlobalExceptionHandler` producing RFC 9457 `ProblemDetails` and `ValidationProblemDetails` responses.
- **OpenAPI Documentation**: Interactive API exploration with Swagger UI.

## Technology Stack

- C# and ASP.NET Core MVC on .NET 10
- Entity Framework Core 10 with Npgsql (PostgreSQL)
- AutoMapper for centralized Entity/DTO mapping
- Swashbuckle for Swagger/OpenAPI
- xUnit, Moq, and EF Core InMemory for unit testing

## Installation and Setup

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and PostgreSQL.
2. From the project root, restore dependencies:

   ```powershell
   dotnet restore
   ```

3. Create a PostgreSQL database named `JobTracker`.
4. Configure the required connection string without adding credentials to source control. In PowerShell, for example:

   ```powershell
   $env:ConnectionStrings__JobTrackerDb = '<POSTGRES_CONNECTION_STRING>'
   ```

5. Apply the existing database migration (install `dotnet-ef` if needed):

   ```powershell
   dotnet tool install --global dotnet-ef --version 10.0.14
   dotnet ef database update
   ```

## Run the Application

From the project root, start the HTTP launch profile:

```powershell
dotnet run --launch-profile http
```

The API listens at `http://localhost:5106`. Swagger UI is available at `http://localhost:5106/swagger`.

## Run Unit Tests

Execute the unit test suite (AutoMapper validation, Services, Controllers, and Validators):

```powershell
dotnet test tests/JobTracker.Api.Tests/JobTracker.Api.Tests.csproj
```
