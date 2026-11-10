# REST API Security, Versioning, CORS & Rate Limiting Reference

## 1. Complete API Infrastructure Registration (`Program.cs`)
```csharp
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace JobTracker.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. ProblemDetails & Exception Handling
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
                ctx.ProblemDetails.Extensions["timestampUtc"] = DateTimeOffset.UtcNow;
            };
        });

        // 2. API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // 3. CORS for Angular Frontend
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy("AngularFrontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("X-Pagination", "X-Correlation-ID");
            });
        });

        // 4. Rate Limiting
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("api-standard", limiter =>
            {
                limiter.PermitLimit = 100;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
        });

        // 5. JWT Bearer Authentication
        var jwtSection = configuration.GetSection("Authentication:Jwt");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        return services;
    }
}
```

---

## 2. Dynamic Sorting & Filtering Implementation
```csharp
public static IQueryable<JobApplication> ApplyFilterAndSort(
    this IQueryable<JobApplication> query,
    JobApplicationQueryParameters parameters)
{
    if (parameters.Status.HasValue)
        query = query.Where(a => a.Status == parameters.Status.Value);

    if (parameters.WorkMode.HasValue)
        query = query.Where(a => a.WorkMode == parameters.WorkMode.Value);

    if (parameters.CompanyId.HasValue)
        query = query.Where(a => a.CompanyId == parameters.CompanyId.Value);

    if (!string.IsNullOrWhiteSpace(parameters.Search))
    {
        var term = parameters.Search.Trim();
        query = query.Where(a =>
            a.JobTitle.Contains(term) ||
            a.Company.Name.Contains(term));
    }

    var isDesc = string.Equals(parameters.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

    return (parameters.SortBy.ToLowerInvariant(), isDesc) switch
    {
        ("jobtitle", true) => query.OrderByDescending(a => a.JobTitle).ThenBy(a => a.Id),
        ("jobtitle", false) => query.OrderBy(a => a.JobTitle).ThenBy(a => a.Id),
        ("companyname", true) => query.OrderByDescending(a => a.Company.Name).ThenBy(a => a.Id),
        ("companyname", false) => query.OrderBy(a => a.Company.Name).ThenBy(a => a.Id),
        ("salarymax", true) => query.OrderByDescending(a => a.SalaryMax).ThenBy(a => a.Id),
        ("salarymax", false) => query.OrderBy(a => a.SalaryMax).ThenBy(a => a.Id),
        ("status", true) => query.OrderByDescending(a => a.Status).ThenBy(a => a.Id),
        ("status", false) => query.OrderBy(a => a.Status).ThenBy(a => a.Id),
        (_, true) => query.OrderByDescending(a => a.AppliedAtUtc).ThenBy(a => a.Id),
        (_, false) => query.OrderBy(a => a.AppliedAtUtc).ThenBy(a => a.Id)
    };
}
```
