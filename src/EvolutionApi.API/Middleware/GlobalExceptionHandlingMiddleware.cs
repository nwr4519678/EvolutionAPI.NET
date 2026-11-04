using EvolutionApi.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace EvolutionApi.API.Middleware;

/// <summary>
/// Global exception handler — last line of defence.
/// Maps typed application exceptions to RFC 7807 ProblemDetails responses.
///
/// Exception hierarchy handled (most specific → least specific):
///   ValidationException            → 400 (field-level errors dictionary)
///   NotFoundException              → 404
///   ConflictException              → 409
///   BusinessRuleException          → 422
///   AppAuthorizationException      → 401
///   ApplicationDomainException     → 400 (fallback for any other domain ex)
///   OperationCanceledException     → 499 (client closed request)
///   Exception                      → 500
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title, detail, errors) = ex switch
        {
            // ── FluentValidation ──────────────────────────────────────────────
            ValidationException vex => (
                (int)HttpStatusCode.BadRequest,
                "Validation Failed",
                "One or more validation errors occurred.",
                (object?)vex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())),

            // ── Typed application exceptions (set by ExceptionHandlingBehavior) ──
            NotFoundException nfe => (
                (int)HttpStatusCode.NotFound,
                "Resource Not Found",
                nfe.Message,
                (object?)null),

            ConflictException ce => (
                (int)HttpStatusCode.Conflict,
                "Conflict",
                ce.Message,
                (object?)null),

            BusinessRuleException bre => (
                422,
                "Business Rule Violation",
                bre.Message,
                (object?)null),

            AppAuthorizationException aae => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                aae.Message,
                (object?)null),

            ApplicationDomainException ade => (
                (int)HttpStatusCode.BadRequest,
                "Application Error",
                ade.Message,
                (object?)null),

            // ── Client cancelled the request (not a server error) ─────────────
            OperationCanceledException when context.RequestAborted.IsCancellationRequested => (
                499,
                "Request Cancelled",
                "The client closed the connection.",
                (object?)null),

            // ── Catch-all ─────────────────────────────────────────────────────
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please contact support.",
                (object?)null)
        };

        // Only log unexpected errors as Error; expected domain errors are Debug/Warning
        if (statusCode >= 500)
            logger.LogError(ex, "[{Status}] Unhandled exception: {Message}", statusCode, ex.Message);
        else if (statusCode >= 400)
            logger.LogWarning("[{Status}] Handled application exception: {Message}", statusCode, ex.Message);

        var problem = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = detail,
            Instance = context.Request.Path
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        context.Response.StatusCode      = statusCode;
        context.Response.ContentType     = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonOpts));
    }
}
