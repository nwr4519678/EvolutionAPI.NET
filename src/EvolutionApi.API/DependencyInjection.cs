using System.Text.Json;
using System.Text.Json.Serialization;
using EvolutionApi.API.Filters;
using EvolutionApi.Application;
using EvolutionApi.Infrastructure;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace EvolutionApi.API;

/// <summary>
/// Presentation/API layer dependency injection registration.
/// Orchestrates all Clean Architecture layers: Application, Infrastructure, and API.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Master DI entry point — registers all layers in dependency order.
    /// Call <c>builder.Services.AddGatewayServices(builder.Configuration)</c> from Program.cs.
    /// </summary>
    public static IServiceCollection AddGatewayServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApplication();          // MediatR, FluentValidation, pipeline behaviors
        services.AddInfrastructure(configuration); // EF Core, caches, WhatsApp engine
        services.AddApiServices(configuration); // MVC, multi-tenancy, OpenAPI, rate limiting

        return services;
    }

    /// <summary>Registers API-layer–specific services.</summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options => options.AddPolicy("dashboard", policy =>
        {
            var origins = configuration.GetSection("Dashboard:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:5160", "https://localhost:7292" };
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }));

        // ── Multi-Tenancy (Finbuckle) ─────────────────────────────────────────
        services.AddMultiTenant<TenantInfo>()
            .WithHeaderStrategy("X-Tenant-Id")
            .WithRouteStrategy("__tenant__", false)
            .WithClaimStrategy("tenant_id")
            .WithInMemoryStore(options =>
            {
                options.Tenants.Add(new TenantInfo
                {
                    Id         = "default",
                    Identifier = "default",
                    Name       = "Default Tenant"
                });
            });

        // ── Action Filters ────────────────────────────────────────────────────
        services.AddScoped<ApiKeyFilter>();

        // ── AutoMapper (Application + API profiles) ───────────────────────────
        // NOTE: AddApplication() does NOT register AutoMapper.
        // A single AddAutoMapper() call here scans both assemblies, avoiding double-registration.
        services.AddAutoMapper(cfg => cfg.AddMaps(
            typeof(DependencyInjection).Assembly,                    // API profiles (none currently)
            typeof(EvolutionApi.Application.DependencyInjection).Assembly)); // Application profiles

        // ── Controllers & JSON ────────────────────────────────────────────────
        services.AddControllers(options =>
            {
                // All MVC endpoints are protected by default.  Health probes explicitly
                // opt out with [AllowAnonymous].
                options.Filters.AddService<ApiKeyFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy    = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition  = JsonIgnoreCondition.WhenWritingNull;
            });

        // ── ProblemDetails (RFC 7807) ─────────────────────────────────────────
        services.AddProblemDetails();

        // ── OpenAPI / Scalar ──────────────────────────────────────────────────
        services.AddEndpointsApiExplorer();
        services.AddOpenApi("v1", options =>
        {
            options.AddSchemaTransformer<JsonElementSchemaTransformer>();
            options.AddDocumentTransformer((doc, _, _) =>
            {
                doc.Info = new()
                {
                    Title       = "Evolution API (.NET Clean Architecture)",
                    Version     = "v1",
                    Description = "High-performance WhatsApp gateway — Clean Architecture, MediatR, SignalR, EF Core, Scalar."
                };
                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>Enables Finbuckle multi-tenant middleware resolution.</summary>
    public static IApplicationBuilder UseApiMultiTenant(this IApplicationBuilder app)
        => app.UseMultiTenant();
}
