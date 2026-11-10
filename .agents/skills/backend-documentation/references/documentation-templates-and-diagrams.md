# Backend Documentation Templates & Runbooks Reference

## 1. Environment Variables & Configuration Template
Use this table format when documenting configuration settings in `README.md`:

| Environment Variable | JSON Configuration Key | Required | Default | Description |
| :--- | :--- | :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | N/A | Yes | `Production` | Runtime environment (`Development`, `Staging`, `Production`). |
| `ConnectionStrings__JobTrackerDb` | `ConnectionStrings:JobTrackerDb` | Yes | — | PostgreSQL or Oracle connection string. |
| `Database__Provider` | `Database:Provider` | No | `PostgreSQL` | Active EF Core provider (`PostgreSQL` or `Oracle`). |
| `Authentication__Jwt__Issuer` | `Authentication:Jwt:Issuer` | Yes | — | JWT token issuer URI. |
| `Authentication__Jwt__Audience` | `Authentication:Jwt:Audience` | Yes | — | JWT token audience identifier. |
| `Authentication__Jwt__SigningKey` | `Authentication:Jwt:SigningKey` | Yes | — | Symmetric signing key (minimum 32 characters; store in User Secrets / Vault). |
| `Cors__AllowedOrigins__0` | `Cors:AllowedOrigins:0` | No | `http://localhost:4200` | Allowed Angular frontend origin. |

---

## 2. API Endpoint Catalog Template

| Method | Endpoint Route | Auth | Request Body / Query | Success Response | Error Responses |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/job-applications` | Bearer JWT | `JobApplicationQueryParameters` | `200 OK` (`PagedResponse<JobApplicationSummaryDto>`) | `400`, `401` |
| `GET` | `/api/v1/job-applications/{id}` | Bearer JWT | Route `id: guid` | `200 OK` (`JobApplicationDetailDto`) | `401`, `404` |
| `POST` | `/api/v1/job-applications` | Bearer JWT | `CreateJobApplicationRequest` | `201 Created` (`JobApplicationDetailDto`) | `400`, `401`, `409` |
| `PUT` | `/api/v1/job-applications/{id}` | Bearer JWT | `UpdateJobApplicationRequest` | `200 OK` (`JobApplicationDetailDto`) | `400`, `401`, `404` |
| `PATCH` | `/api/v1/job-applications/{id}/status` | Bearer JWT | `TransitionStatusRequest` | `200 OK` (`JobApplicationSummaryDto`) | `400`, `401`, `404`, `409` |
| `DELETE` | `/api/v1/job-applications/{id}` | Bearer JWT | Route `id: guid` | `204 No Content` | `401`, `404` |

---

## 3. Troubleshooting Runbook Template

### Issue 1: `dotnet ef` Fails with `Unable to create a 'DbContext'`
- **Symptom**: Running `dotnet ef migrations add` fails at design time.
- **Resolution**:
  1. Ensure `--project` points to the Infrastructure project and `--startup-project` points to the API project.
  2. Verify `Microsoft.EntityFrameworkCore.Design` is installed in the startup project without excluding runtime assets needed by `dotnet-ef`.

### Issue 2: Angular Frontend Receives CORS Error on `localhost:4200`
- **Symptom**: Browser console reports `Access-Control-Allow-Origin` missing on preflight `OPTIONS` or `POST` requests.
- **Resolution**:
  1. Confirm `http://localhost:4200` is present in `Cors:AllowedOrigins` in `appsettings.Development.json`.
  2. Confirm `app.UseCors("AngularFrontend")` is registered **before** `app.UseAuthentication()` and `app.UseAuthorization()` in `Program.cs`.

---

## 4. `CHANGELOG.md` Template (Keep a Changelog + SemVer)
```markdown
# Changelog

All notable changes to the Job Tracker Backend will be documented in this file.

## [Unreleased]

### Added
- Initial .NET 10 Web API architecture and Agent Skills library.
- Job Application, Company, InterviewRound, OfferDetails, and SkillTag domain models and DTO contracts.

### Changed
- Standardized JSON serialization on `System.Text.Json` with `camelCase` and `JsonStringEnumConverter`.

### Fixed
- Prevented N+1 queries on job application detail endpoints using `.AsSplitQuery()` and DTO projections.
```
