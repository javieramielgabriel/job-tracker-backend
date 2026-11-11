using System.Text.Json.Serialization;
using JobTracker.Api.Common.Constants;
using JobTracker.Api.Common.Middleware;
using JobTracker.Api.Data;
using JobTracker.Api.Mappings;
using JobTracker.Api.Models;
using JobTracker.Api.Services;
using JobTracker.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Configuration;

/// <summary>
/// Centralized Dependency Injection registration extensions organized by architectural layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the EF Core DbContext and database persistence services.
    /// </summary>
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ApiConstants.DatabaseConnectionStringName)
            ?? throw new InvalidOperationException(
                $"The {ApiConstants.DatabaseConnectionStringName} connection string is not configured.");

        services.AddDbContext<JobTrackerDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    /// <summary>
    /// Registers AutoMapper profiles, time provider, and application business services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddAutoMapper(cfg =>
            cfg.AddMaps(typeof(JobApplicationMappingProfile).Assembly));

        services.AddScoped<IJobApplicationService, JobApplicationService>();
        services.AddScoped<IInterviewPhaseService, InterviewPhaseService>();

        return services;
    }

    /// <summary>
    /// Registers MVC controllers, JSON serialization options, ProblemDetails, global exception handling, CORS, and OpenAPI/Swagger.
    /// </summary>
    public static IServiceCollection AddApiPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var enumConverter = new JsonStringEnumConverter<ApplicationStatus>(allowIntegerValues: false);

        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(enumConverter);
            });

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(enumConverter);
        });

        var corsSettings = configuration
            .GetSection(CorsSettings.SectionName)
            .Get<CorsSettings>() ?? new CorsSettings();

        var allowedOrigins = corsSettings.AllowedOrigins.Length > 0
            ? corsSettings.AllowedOrigins
            : ["http://localhost:4200"];

        services.AddCors(options =>
        {
            options.AddPolicy(ApiConstants.AngularCorsPolicy, policy =>
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }
}
