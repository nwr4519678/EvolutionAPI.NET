using EvolutionApi.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvolutionApi.API.Controllers;

/// <summary>
/// Health check controller.
/// Thin controller — infrastructure liveness and readiness probe checks.
/// </summary>
[ApiController]
[Route("health")]
[AllowAnonymous]
public sealed class HealthController(IUnitOfWork uow) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "alive" });

    [HttpGet("live")]
    public IActionResult Live() => Ok(new { status = "alive" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        try
        {
            var canConnect = await uow.CanConnectAsync(ct);
            return canConnect
                ? Ok(new { status = "ready", database = "connected" })
                : StatusCode(503, new { status = "not-ready", database = "unavailable" });
        }
        catch
        {
            return StatusCode(503, new { status = "not-ready", database = "unavailable" });
        }
    }
}
