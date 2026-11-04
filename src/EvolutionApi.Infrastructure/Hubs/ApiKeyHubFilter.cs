using EvolutionApi.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EvolutionApi.Infrastructure.Hubs;

/// <summary>
/// Requires the global gateway API key before a SignalR connection is accepted.
/// Instance-scoped keys are intentionally not accepted here because a hub
/// connection can subscribe to multiple instance streams.
/// </summary>
public sealed class ApiKeyHubFilter(IApiKeyValidator apiKeyValidator) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
        => await next(invocationContext);

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var httpContext = context.Context.GetHttpContext();
        var apiKey = httpContext?.Request.Headers["apikey"].FirstOrDefault();
        var routePath = httpContext?.Request.Path.Value;
        var validation = await apiKeyValidator.ValidateApiKeyAsync(
            new ApiKeyValidationRequest(apiKey, null, routePath), context.Context.ConnectionAborted);

        if (!validation.IsValid)
        {
            context.Context.Abort();
            throw new HubException("Unauthorized");
        }

        await next(context);
    }

    public Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
        => next(context, exception);
}
