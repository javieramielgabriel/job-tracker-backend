---
name: dotnet-json-serialization
description: >-
  Use this skill when configuring or troubleshooting JSON serialization and
  deserialization in .NET using System.Text.Json. Covers JsonSerializer,
  JsonSerializerOptions, camelCase PropertyNamingPolicy, JsonPropertyName,
  string enum serialization, custom JsonConverter<T>, DateTimeOffset/DateOnly
  ISO-8601 handling, nullable properties, circular reference prevention, and
  Angular frontend contract compatibility.
---

# JSON Serialization and API Data Handling (`dotnet-json-serialization`)

## Overview
Provides standards and patterns for high-performance, predictable JSON serialization and deserialization using **`System.Text.Json`** in the **Job Tracker Backend**, ensuring seamless TypeScript/Angular frontend contract compatibility.

## Activation Instructions
Activate this skill whenever you are:
- Configuring `JsonSerializerOptions` for ASP.NET Core Controllers (`AddJsonOptions`), Minimal APIs (`ConfigureHttpJsonOptions`), `HttpClient`, or CLI JSON output.
- Customizing property names (`[JsonPropertyName]`), enum representations (`JsonStringEnumConverter`), null handling, or number handling.
- Handling `DateTimeOffset`, `DateTime`, `DateOnly`, `TimeOnly`, or custom value objects via `JsonConverter<T>`.
- Diagnosing serialization exceptions, circular object graphs, or payload mismatches between the .NET backend and the Angular frontend.

## Specific Responsibilities
1. **Standardize on `System.Text.Json` with Web Defaults**: Use `camelCase` property naming, case-insensitive deserialization, and string-based enums (`JsonStringEnumConverter`) so Angular TypeScript models consume readable, self-describing payloads.
2. **Use ISO-8601 UTC Timestamps (`DateTimeOffset`) & `DateOnly`**: Always serialize point-in-time timestamps as UTC ISO-8601 strings (`2026-11-10T08:30:00Z`) using `DateTimeOffset` and calendar dates (`2026-11-10`) using `DateOnly`.
3. **Prevent Circular References at the Architectural Level**: Project entities to acyclic DTOs instead of relying on `ReferenceHandler.Preserve` (which pollutes JSON with `$id` and `$values` metadata that breaks Angular interfaces).
4. **Fail Fast on Malformed Payloads**: Reject unmapped enum values or invalid types with clear `400 Bad Request` `ValidationProblemDetails`.

---

## Technology-Specific Implementation Guidance

### 1. Unified `JsonSerializerOptions` Configuration in ASP.NET Core
Configure both Controllers (`AddControllers().AddJsonOptions(...)`) and Minimal APIs (`ConfigureHttpJsonOptions(...)`) identically so all endpoints share the exact same contract behavior:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobTracker.Api.Extensions;

public static class JsonConfigurationExtensions
{
    public static IServiceCollection AddStandardJsonSerialization(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options => ConfigureOptions(options.SerializerOptions));

        services.AddControllers()
            .AddJsonOptions(options => ConfigureOptions(options.JsonSerializerOptions));

        return services;
    }

    public static void ConfigureOptions(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.NumberHandling = JsonNumberHandling.Strict;

        // Serialize enums as PascalCase strings (e.g., "Interviewing", "Remote") for Angular
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
```

### 2. Controlling DTO Serialization with Attributes
```csharp
using System.Text.Json.Serialization;
using JobTracker.Domain.Entities;

namespace JobTracker.Application.Contracts.Applications;

public sealed record JobApplicationSummaryDto
{
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    [JsonPropertyName("jobTitle")]
    public required string JobTitle { get; init; }

    [JsonPropertyName("companyName")]
    public required string CompanyName { get; init; }

    [JsonPropertyName("status")]
    public required ApplicationStatus Status { get; init; }

    [JsonPropertyName("workMode")]
    public required WorkMode WorkMode { get; init; }

    // Omitted from JSON when null (or include explicitly if frontend prefers explicit null)
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? SalaryMax { get; init; }

    [JsonPropertyName("appliedAtUtc")]
    public required DateTimeOffset AppliedAtUtc { get; init; }
}
```

### 3. Custom `JsonConverter<T>` for Trimming Strings or Value Objects
Create a strongly typed `JsonConverter<T>` when custom parsing, trimming, or domain value object serialization is needed:
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobTracker.Api.Serialization;

public sealed class TrimmingStringJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}
```

See [Custom Converters and Angular Contract Reference](./references/custom-converters-and-contracts.md) for `JsonSerializerContext` source generation, polymorphic serialization (`[JsonDerivedType]`), and Angular TypeScript mapping.

---

## Common Mistakes to Avoid
1. **Using `Newtonsoft.Json` Attributes (`[ Newtonsoft.Json.JsonProperty ]`) with `System.Text.Json`**: `System.Text.Json` ignores `Newtonsoft.Json` attributes silently! Always use `System.Text.Json.Serialization` attributes (`[JsonPropertyName]`, `[JsonIgnore]`, `[JsonConverter]`).
2. **Configuring Only `ConfigureHttpJsonOptions` When Using Controllers**: `ConfigureHttpJsonOptions` applies to Minimal APIs and `TypedResults`, whereas MVC Controllers use `AddControllers().AddJsonOptions(...)`. Configure both!
3. **Using `ReferenceHandler.Preserve` for Angular APIs**: Emits `{"$id": "1", "$values": [...]}` wrapper objects that break standard TypeScript arrays in Angular `HttpClient`. Use flat/nested DTO projections and `ReferenceHandler.IgnoreCycles` as a safety net.
4. **Using `DateTime` with `DateTimeKind.Unspecified`**: Causes timezone shifts between the database, backend, and browser. Always use `DateTimeOffset` in UTC (`2026-11-10T12:00:00Z`) or `DateOnly` for calendar dates (`2026-11-10`).

---

## Verification Checklist
- [ ] All JSON attributes come from `System.Text.Json.Serialization` (no `Newtonsoft.Json` mix-ups).
- [ ] Both MVC Controllers and Minimal APIs share identical `JsonSerializerOptions`.
- [ ] Enums serialize as strings (`"Applied"`, `"Interviewing"`) matching Angular TypeScript union types/enums.
- [ ] Timestamps use `DateTimeOffset` (UTC) and date-only fields use `DateOnly`.
- [ ] No circular entity graphs are serialized; responses use dedicated DTOs.

---

## Official Documentation References
- [System.Text.Json Overview](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)
- [How to Customize Property Names and Values with System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties)
- [How to Write Custom Converters for JSON Serialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to)
