using EvolutionApi.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EvolutionApi.API.Filters;

/// <summary>
/// Convenience attribute to apply API key authentication to a controller or action.
/// Usage: <c>[ApiKeyAuthorize]</c>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiKeyAuthorizeAttribute : ServiceFilterAttribute
{
    public ApiKeyAuthorizeAttribute() : base(typeof(ApiKeyFilter)) { }
}

/// <summary>
/// API key authorization action filter.
/// Pure presentation adapter — delegates all business logic to <see cref="IApiKeyValidator"/>.
/// Returns RFC 7807 ProblemDetails on failure, consistent with GlobalExceptionHandlingMiddleware.
/// </summary>
public sealed class ApiKeyFilter(IApiKeyValidator apiKeyValidator) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Kubernetes/load-balancer probes are the only deliberately anonymous MVC
        // surface.  Do not accidentally authenticate an action marked anonymous.
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            await next();
            return;
        }

        var apiKey       = context.HttpContext.Request.Headers["apikey"].FirstOrDefault();
        var instanceName = context.RouteData.Values.GetValueOrDefault("instanceName")?.ToString();
        var routePath    = context.HttpContext.Request.Path.Value;

        var validationResult = await apiKeyValidator.ValidateApiKeyAsync(
            new ApiKeyValidationRequest(apiKey, instanceName, routePath),
            context.HttpContext.RequestAborted);

        if (!validationResult.IsValid)
        {
            // Return RFC 7807 ProblemDetails — consistent with GlobalExceptionHandlingMiddleware
            var problem = new ProblemDetails
            {
                Status   = StatusCodes.Status401Unauthorized,
                Type     = "https://httpstatuses.com/401",
                Title    = "Unauthorized",
                Detail   = validationResult.ErrorMessage ?? "Invalid or missing apikey header",
                Instance = routePath
            };

            context.Result = new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        await next();
    }
}
