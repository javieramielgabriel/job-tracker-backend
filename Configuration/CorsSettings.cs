namespace JobTracker.Api.Configuration;

/// <summary>
/// Strongly-typed configuration options for Cross-Origin Resource Sharing (CORS).
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = ["http://localhost:4200"];
}
