using EvolutionApi.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EvolutionApi.Application;

/// <summary>
/// Application layer DI registration.
/// Call services.AddApplication() from the API layer's DependencyInjection.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // ── MediatR ─────────────────────────────────────────────────────────────
        // Pipeline execution order (outermost → innermost → handler):
        //
        //   1. TenantValidationBehavior  — resolve & enforce tenant context
        //   2. PerformanceBehavior       — timing + OpenTelemetry Activity spans
        //   3. ExceptionHandlingBehavior — translate raw exceptions → typed AppExceptions
        //   4. CachingBehavior           — serve from HybridCache on cache-hit (skips 5)
        //   5. ValidationBehavior        — FluentValidation (async, concurrent)
        //   6. → Handler
        //
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TenantValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // ── FluentValidation ─────────────────────────────────────────────────────
        // Auto-registers all IValidator<T> implementations in this assembly
        services.AddValidatorsFromAssembly(assembly);

        // ── AutoMapper ───────────────────────────────────────────────────────────
        // Auto-discovers all Profile subclasses in this assembly
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        return services;
    }
}
