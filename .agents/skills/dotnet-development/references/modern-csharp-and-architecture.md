# Modern C# and ASP.NET Core Architecture Reference

## 1. Modern C# Idioms (.NET 10 / C# 13 & 14)

### Records, Required Members, and Collection Expressions
```csharp
namespace JobTracker.Domain.ValueObjects;

public sealed record SalaryRange
{
    public required decimal Minimum { get; init; }
    public required decimal Maximum { get; init; }
    public required string Currency { get; init; } = "USD";

    public static SalaryRange Create(decimal min, decimal max, string currency = "USD")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(min);
        ArgumentOutOfRangeException.ThrowIfLessThan(max, min);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return new SalaryRange
        {
            Minimum = min,
            Maximum = max,
            Currency = currency.ToUpperInvariant()
        };
    }
}
```

### Generic Constraints and Extension Methods
```csharp
namespace JobTracker.Shared.Extensions;

public static class QueryablePaginationExtensions
{
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int pageNumber, int pageSize)
        where T : class
    {
        var normalizedPage = Math.Max(1, pageNumber);
        var normalizedSize = Math.Clamp(pageSize, 1, 100);
        return query.Skip((normalizedPage - 1) * normalizedSize).Take(normalizedSize);
    }
}
```

---

## 2. Custom Middleware Pattern
Prefer implementing `IMiddleware` or high-performance request delegates for cross-cutting concerns like correlation IDs:
```csharp
namespace JobTracker.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(HeaderName, correlationId);
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
```

---

## 3. Authentication, Authorization & OpenAPI in .NET 10
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication()
    .AddJwtBearer();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CandidateOnly", policy => policy.RequireRole("Candidate", "Admin"));

// .NET 9/10 built-in OpenAPI support alongside Swagger UI
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.Run();
```
