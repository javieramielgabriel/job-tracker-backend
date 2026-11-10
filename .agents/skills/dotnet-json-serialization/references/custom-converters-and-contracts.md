# System.Text.Json Custom Converters & Angular Contract Compatibility

## 1. .NET to Angular TypeScript Contract Alignment

| .NET Type (`C#`) | JSON Representation | Angular TypeScript Type | Notes |
| :--- | :--- | :--- | :--- |
| `Guid` | `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` | `string` | UUID v4/v7 string |
| `ApplicationStatus` (enum) | `"Interviewing"` | `'Draft' \| 'Applied' \| 'Interviewing' \| ...` | Via `JsonStringEnumConverter` |
| `decimal?` | `145000.00` or `null` | `number \| null` | Use `JsonNumberHandling.Strict` |
| `DateTimeOffset` | `"2026-11-10T16:20:00+00:00"` | `string` (ISO-8601) | Parsed via `new Date(dto.appliedAtUtc)` or `Intl.DateTimeFormat` |
| `DateOnly` | `"2026-11-30"` | `string` (`YYYY-MM-DD`) | No timezone shift issues for offer expiration dates |
| `IReadOnlyList<T>` | `[ ... ]` | `readonly T[]` | Never wrap arrays in `$values` |

---

## 2. Polymorphic Serialization in .NET 8/9/10 (`[JsonPolymorphic]`)
For activity timeline feeds or notification payloads with multiple subtypes:
```csharp
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "eventType")]
[JsonDerivedType(typeof(StatusChangedTimelineEventDto), typeDiscriminator: "statusChanged")]
[JsonDerivedType(typeof(InterviewScheduledTimelineEventDto), typeDiscriminator: "interviewScheduled")]
public abstract record TimelineEventDto(Guid Id, DateTimeOffset OccurredAtUtc);

public sealed record StatusChangedTimelineEventDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    ApplicationStatus PreviousStatus,
    ApplicationStatus NewStatus) : TimelineEventDto(Id, OccurredAtUtc);

public sealed record InterviewScheduledTimelineEventDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    int RoundNumber,
    string StageName,
    DateTimeOffset ScheduledAtUtc) : TimelineEventDto(Id, OccurredAtUtc);
```

---

## 3. Source-Generated `JsonSerializerContext` (High Performance / AOT Ready)
```csharp
using System.Text.Json.Serialization;
using JobTracker.Application.Contracts.Applications;

namespace JobTracker.Api.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(JobApplicationDetailDto))]
[JsonSerializable(typeof(PagedResponse<JobApplicationSummaryDto>))]
[JsonSerializable(typeof(CreateJobApplicationRequest))]
[JsonSerializable(typeof(UpdateJobApplicationRequest))]
public partial class JobTrackerJsonContext : JsonSerializerContext;
```
