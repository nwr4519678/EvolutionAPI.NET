using EvolutionApi.Application.Common.Interfaces;
using Finbuckle.MultiTenant.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces multi-tenant boundaries.
/// - Implicitly injects resolved TenantId into tenant-scoped commands.
/// - Strictly prevents cross-tenant instance tampering.
/// </summary>
public sealed class TenantValidationBehavior<TRequest, TResponse>(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    ILogger<TenantValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is ITenantScopedRequest tenantScopedRequest)
        {
            var resolvedTenantId = multiTenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

            if (!string.IsNullOrWhiteSpace(resolvedTenantId))
            {
                if (string.IsNullOrWhiteSpace(tenantScopedRequest.TenantId))
                {
                    tenantScopedRequest.TenantId = resolvedTenantId;
                    logger.LogDebug("Implicitly bound TenantId '{TenantId}' to request {RequestName}",
                        resolvedTenantId, typeof(TRequest).Name);
                }
                else if (!string.Equals(tenantScopedRequest.TenantId, resolvedTenantId, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Cross-tenant tampering detected! Request TenantId '{ReqTenant}' vs Resolved TenantId '{CtxTenant}'",
                        tenantScopedRequest.TenantId, resolvedTenantId);

                    throw new UnauthorizedAccessException(
                        $"Cross-tenant access violation: Requested tenant '{tenantScopedRequest.TenantId}' does not match context tenant '{resolvedTenantId}'.");
                }
            }
            else if (string.IsNullOrWhiteSpace(tenantScopedRequest.TenantId))
            {
                logger.LogWarning("Tenant-scoped request {RequestName} dispatched without an identifiable tenant context",
                    typeof(TRequest).Name);

                throw new UnauthorizedAccessException(
                    "Tenant context is required but could not be resolved from HTTP headers, route, or token.");
            }
        }

        return await next();
    }
}
