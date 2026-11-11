using JobTracker.Api.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Common.Middleware;

/// <summary>
/// Centralized exception handler that translates application, validation, and concurrency exceptions
/// into standard RFC 9457 ProblemDetails HTTP responses.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails;

        switch (exception)
        {
            case DomainValidationException validationException:
                logger.LogWarning(
                    "Validation failed for {Method} {Path}: {Title}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    validationException.Title);

                problemDetails = new HttpValidationProblemDetails(validationException.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = validationException.Title,
                    Detail = validationException.Message,
                    Instance = httpContext.Request.Path
                };
                break;

            case AppException appException:
                logger.LogWarning(
                    "Application error ({StatusCode}) for {Method} {Path}: {Detail}",
                    appException.StatusCode,
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    appException.Message);

                problemDetails = new ProblemDetails
                {
                    Status = appException.StatusCode,
                    Title = appException.Title,
                    Detail = appException.Message,
                    Instance = httpContext.Request.Path
                };
                break;

            case DbUpdateConcurrencyException concurrencyException:
                logger.LogWarning(
                    concurrencyException,
                    "Database concurrency conflict for {Method} {Path}",
                    httpContext.Request.Method,
                    httpContext.Request.Path);

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Concurrency conflict",
                    Detail = "The resource was modified by another request. Please refresh and try again.",
                    Instance = httpContext.Request.Path
                };
                break;

            default:
                logger.LogError(
                    exception,
                    "Unhandled exception occurred while processing {Method} {Path}",
                    httpContext.Request.Method,
                    httpContext.Request.Path);

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while processing your request.",
                    Instance = httpContext.Request.Path
                };
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
