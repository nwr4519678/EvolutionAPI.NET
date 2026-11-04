using EvolutionApi.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that translates known infrastructure and domain exceptions into
/// structured <see cref="ApplicationDomainException"/> subtypes so the API layer can
/// map them cleanly to ProblemDetails without leaking stack traces.
///
/// Execution order in pipeline:
///   TenantValidation → Performance → ExceptionHandling → Caching → Validation → Handler
///
/// Exception mapping:
///   DomainRuleViolationException    → 422 (business rule, typed)
///   InstanceNotConnectedException   → 422 (instance lifecycle guard)
///   TenantContextMissingException   → 401 (tenant auth)
///   KeyNotFoundException            → 404
///   InvalidOperationException       → 422 (business rule, generic)
///   UnauthorizedAccessException     → 401
///   FluentValidation.ValidationException → 400 (belt-and-suspenders, handled by ValidationBehavior first)
///   OperationCanceledException      → re-thrown silently
///   Everything else                 → re-thrown (caught by GlobalExceptionHandlingMiddleware → 500)
/// </summary>
public sealed class ExceptionHandlingBehavior<TRequest, TResponse>(
    ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (ValidationException)
        {
            // Already structured by ValidationBehavior — let it propagate as-is
            throw;
        }
        // ── Domain-typed exceptions (highest priority) ─────────────────────────
        catch (DomainRuleViolationException ex)
        {
            logger.LogWarning(
                "Domain rule [{Rule}] violated in {Request}: {Message}",
                ex.RuleCode, typeof(TRequest).Name, ex.Message);
            throw new BusinessRuleException($"[{ex.RuleCode}] {ex.Message}", ex);
        }
        catch (InstanceNotConnectedException ex)
        {
            logger.LogWarning(
                "Instance '{Instance}' not connected during {Request}",
                ex.InstanceName, typeof(TRequest).Name);
            throw new BusinessRuleException(ex.Message, ex);
        }
        catch (TenantContextMissingException ex)
        {
            logger.LogWarning(
                "Tenant context missing during {Request}: {Message}",
                typeof(TRequest).Name, ex.Message);
            throw new AppAuthorizationException(ex.Message, ex);
        }
        // ── Framework exceptions ───────────────────────────────────────────────
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(
                "Resource not found during {Request}: {Message}",
                typeof(TRequest).Name, ex.Message);
            throw new NotFoundException(ex.Message, ex);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                "Business rule violation during {Request}: {Message}",
                typeof(TRequest).Name, ex.Message);
            throw new BusinessRuleException(ex.Message, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(
                "Authorization failure during {Request}: {Message}",
                typeof(TRequest).Name, ex.Message);
            throw new AppAuthorizationException(ex.Message, ex);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogDebug("Request {Request} was cancelled.", typeof(TRequest).Name);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in {Request}", typeof(TRequest).Name);
            throw;
        }
    }
}

// ── Application-layer typed exception hierarchy ───────────────────────────────

/// <summary>Base class for all structured application exceptions.</summary>
public abstract class ApplicationDomainException(string message, Exception? inner = null)
    : Exception(message, inner);

/// <summary>Thrown when a requested resource does not exist (maps to HTTP 404).</summary>
public sealed class NotFoundException(string message, Exception? inner = null)
    : ApplicationDomainException(message, inner);

/// <summary>Thrown when a business invariant is violated (maps to HTTP 422).</summary>
public sealed class BusinessRuleException(string message, Exception? inner = null)
    : ApplicationDomainException(message, inner);

/// <summary>Thrown when an action is not permitted (maps to HTTP 401/403).</summary>
public sealed class AppAuthorizationException(string message, Exception? inner = null)
    : ApplicationDomainException(message, inner);

/// <summary>Thrown when a duplicate/conflict is detected (maps to HTTP 409).</summary>
public sealed class ConflictException(string message, Exception? inner = null)
    : ApplicationDomainException(message, inner);
