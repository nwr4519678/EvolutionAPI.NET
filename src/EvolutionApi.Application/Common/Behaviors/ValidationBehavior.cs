using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all registered <see cref="IValidator{T}"/>
/// instances asynchronously before the handler executes.
///
/// - Runs validators in parallel for maximum throughput.
/// - Throws <see cref="ValidationException"/> with all failures aggregated (not fail-fast).
/// - Short-circuits immediately if no validators are registered for the request type.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var validatorList = validators.ToList();
        if (validatorList.Count == 0) return await next();

        var context = new ValidationContext<TRequest>(request);

        // Run all validators concurrently — aggregate ALL failures (not fail-fast)
        var results = await Task.WhenAll(
            validatorList.Select(v => v.ValidateAsync(context, ct)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            logger.LogWarning(
                "[VALIDATION] Failed for {Request}: {Errors}",
                typeof(TRequest).Name,
                string.Join("; ", failures.Select(f => f.ErrorMessage)));

            throw new ValidationException(failures);
        }

        return await next();
    }
}
