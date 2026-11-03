using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EvolutionApi.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that provides structured performance observability:
/// - Logs every request at Debug with handler timing.
/// - Emits a structured Warning for any request exceeding <see cref="SlowRequestThresholdMs"/>.
/// - Records an <see cref="Activity"/> span compatible with OpenTelemetry / Jaeger / Zipkin.
///
/// Sits early in the pipeline (after TenantValidation) so it captures total
/// handler + validation + caching time.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    private static readonly ActivitySource ActivitySource =
        new("EvolutionApi.Application", "1.0.0");

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        using var activity = ActivitySource.StartActivity(requestName, ActivityKind.Internal);
        activity?.SetTag("request.type", requestName);

        logger.LogDebug("[PERF] → Handling {Request}", requestName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();

            activity?.SetTag("request.elapsed_ms", sw.ElapsedMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
            {
                logger.LogWarning(
                    "[PERF] Slow request detected — {Request} completed in {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                    requestName, sw.ElapsedMilliseconds, SlowRequestThresholdMs);
            }
            else
            {
                logger.LogDebug(
                    "[PERF] ← {Request} completed in {ElapsedMs}ms",
                    requestName, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetTag("request.elapsed_ms", sw.ElapsedMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            logger.LogError(
                ex,
                "[PERF] {Request} failed after {ElapsedMs}ms",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
