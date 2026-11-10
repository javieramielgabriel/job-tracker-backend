# Job Tracker Backend

A REST API for managing job applications and their interview phases. Applications include company and job details, status, optional notes, and application dates.

## Key features

- Create, list, retrieve, update, and delete job applications.
- Include multiple interview phases with optional schedules and notes.
- Browse the API with Swagger UI.

## Technology stack

- C# and ASP.NET Core on .NET 10
- Entity Framework Core 10
- PostgreSQL with the Npgsql provider
- Swashbuckle for Swagger/OpenAPI

## Installation and setup

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

## Run the application

From the project root, start the HTTP launch profile:

```powershell
dotnet run --launch-profile http
```

The API listens at `http://localhost:5106`. Swagger UI is available at `http://localhost:5106/swagger`.
